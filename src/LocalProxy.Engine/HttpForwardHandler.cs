using System.Net;
using System.Net.Sockets;
using System.Text;
using LocalProxy.Core;
using Microsoft.Extensions.Logging;

namespace LocalProxy.Engine;

public sealed class HttpForwardHandler : ITunnelHandler
{
    private readonly ProxyConfig _config;
    private readonly ILogger<HttpForwardHandler> _logger;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _stopCts = new();
    private readonly SemaphoreSlim _connectionLimit;
    private long _bytesIn;
    private long _bytesOut;
    private int _activeConnections;
    private TunnelStatus _status = TunnelStatus.Stopped;
    private DateTime _startTime;

    public string Name => _config.Name;

    public HttpForwardHandler(ProxyConfig config, ILogger<HttpForwardHandler> logger)
    {
        _config = config;
        _logger = logger;
        _connectionLimit = new SemaphoreSlim(config.MaxConnections);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _status = TunnelStatus.Starting;
        _listener = new TcpListener(IPAddress.Any, _config.LocalPort);
        _listener.Start();
        _startTime = DateTime.UtcNow;
        _status = TunnelStatus.Running;

        _logger.LogInformation(
            "HTTP tunnel '{Name}' started on port {LocalPort} -> {RemoteHost}:{RemotePort}",
            Name, _config.LocalPort, _config.RemoteHost, _config.RemotePort);

        try
        {
            while (!_stopCts.Token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_stopCts.Token);
                _ = HandleClientAsync(client);
            }
        }
        catch (OperationCanceledException) when (_stopCts.Token.IsCancellationRequested) { }
        catch (ObjectDisposedException) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _status = TunnelStatus.Stopping;
        _stopCts.Cancel();
        _listener?.Stop();

        while (Volatile.Read(ref _activeConnections) > 0 && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }

        _listener?.Dispose();
        _listener = null;
        _status = TunnelStatus.Stopped;

        _logger.LogInformation(
            "HTTP tunnel '{Name}' stopped. Total: {BytesIn} bytes in, {BytesOut} bytes out",
            Name, Interlocked.Read(ref _bytesIn), Interlocked.Read(ref _bytesOut));
    }

    public TunnelStats GetStats()
    {
        return new TunnelStats
        {
            BytesIn = Interlocked.Read(ref _bytesIn),
            BytesOut = Interlocked.Read(ref _bytesOut),
            ActiveConnections = Volatile.Read(ref _activeConnections),
            Uptime = _status == TunnelStatus.Running ? DateTime.UtcNow - _startTime : TimeSpan.Zero,
            Status = _status
        };
    }

    private async Task HandleClientAsync(TcpClient localClient)
    {
        if (!await _connectionLimit.WaitAsync(TimeSpan.FromSeconds(3)))
        {
            localClient.Dispose();
            return;
        }

        Interlocked.Increment(ref _activeConnections);

        try
        {
            using var remoteClient = new TcpClient();
            await remoteClient.ConnectAsync(_config.RemoteHost, _config.RemotePort);

            using var localStream = localClient.GetStream();
            using var remoteStream = remoteClient.GetStream();

            var cts = CancellationTokenSource.CreateLinkedTokenSource(_stopCts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            // Bidirectional raw forwarding (HTTP is TCP-based)
            var localToRemote = CopyStreamAsync(localStream, remoteStream, isRemote: false, cts.Token);
            var remoteToLocal = CopyStreamAsync(remoteStream, localStream, isRemote: true, cts.Token);

            await Task.WhenAny(localToRemote, remoteToLocal);
        }
        catch (SocketException ex)
        {
            _logger.LogDebug("HTTP tunnel '{Name}': connection error: {Message}", Name, ex.Message);
        }
        catch (OperationCanceledException) { }
        finally
        {
            localClient.Dispose();
            Interlocked.Decrement(ref _activeConnections);
            _connectionLimit.Release();
        }
    }

    private async Task CopyStreamAsync(NetworkStream source, NetworkStream dest, bool isRemote, CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, ct)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                if (isRemote)
                    Interlocked.Add(ref _bytesIn, bytesRead);
                else
                    Interlocked.Add(ref _bytesOut, bytesRead);
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
    }

    public void Dispose()
    {
        _stopCts.Dispose();
        _connectionLimit.Dispose();
        _listener?.Dispose();
    }
}
