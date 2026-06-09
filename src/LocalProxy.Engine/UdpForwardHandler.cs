using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using LocalProxy.Core;
using Microsoft.Extensions.Logging;

namespace LocalProxy.Engine;

public sealed class UdpForwardHandler : ITunnelHandler
{
    private readonly ProxyConfig _config;
    private readonly ILogger<UdpForwardHandler> _logger;
    private UdpClient? _localClient;
    private readonly CancellationTokenSource _stopCts = new();
    private long _bytesIn;
    private long _bytesOut;
    private TunnelStatus _status = TunnelStatus.Stopped;
    private DateTime _startTime;
    private readonly ConcurrentDictionary<IPEndPoint, UdpClient> _sessions = new();

    public string Name => _config.Name;

    public UdpForwardHandler(ProxyConfig config, ILogger<UdpForwardHandler> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _status = TunnelStatus.Starting;
        _localClient = new UdpClient(_config.LocalPort);
        _startTime = DateTime.UtcNow;
        _status = TunnelStatus.Running;

        _logger.LogInformation(
            "UDP tunnel '{Name}' started on port {LocalPort} -> {RemoteHost}:{RemotePort}",
            Name, _config.LocalPort, _config.RemoteHost, _config.RemotePort);

        try
        {
            while (!_stopCts.Token.IsCancellationRequested)
            {
                var result = await _localClient.ReceiveAsync(_stopCts.Token);
                _ = ForwardDatagramAsync(result.Buffer, result.RemoteEndPoint);
            }
        }
        catch (OperationCanceledException) when (_stopCts.Token.IsCancellationRequested) { }
        catch (ObjectDisposedException) { }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _status = TunnelStatus.Stopping;
        _stopCts.Cancel();
        _localClient?.Dispose();

        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }
        _sessions.Clear();

        _status = TunnelStatus.Stopped;
        _logger.LogInformation(
            "UDP tunnel '{Name}' stopped. Total: {BytesIn} bytes in, {BytesOut} bytes out",
            Name, Interlocked.Read(ref _bytesIn), Interlocked.Read(ref _bytesOut));

        await Task.CompletedTask;
    }

    public TunnelStats GetStats()
    {
        return new TunnelStats
        {
            BytesIn = Interlocked.Read(ref _bytesIn),
            BytesOut = Interlocked.Read(ref _bytesOut),
            ActiveConnections = _sessions.Count,
            Uptime = _status == TunnelStatus.Running ? DateTime.UtcNow - _startTime : TimeSpan.Zero,
            Status = _status
        };
    }

    private async Task ForwardDatagramAsync(byte[] data, IPEndPoint clientEndpoint)
    {
        try
        {
            var remoteClient = GetOrCreateSession(clientEndpoint);
            await remoteClient.SendAsync(data, _config.RemoteHost, _config.RemotePort);
            Interlocked.Add(ref _bytesOut, data.Length);

            // Wait for response
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_stopCts.Token);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

            var response = await remoteClient.ReceiveAsync(timeoutCts.Token);
            Interlocked.Add(ref _bytesIn, response.Buffer.Length);

            // Send response back to original client
            if (_localClient != null)
            {
                await _localClient.SendAsync(response.Buffer, clientEndpoint);
                Interlocked.Add(ref _bytesOut, response.Buffer.Length);
            }

            CleanupSession(clientEndpoint);
        }
        catch (OperationCanceledException) { }
        catch (SocketException ex)
        {
            _logger.LogDebug("UDP tunnel '{Name}': error: {Message}", Name, ex.Message);
        }
    }

    private UdpClient GetOrCreateSession(IPEndPoint clientEndpoint)
    {
        return _sessions.GetOrAdd(clientEndpoint, _ => new UdpClient(0));
    }

    private void CleanupSession(IPEndPoint clientEndpoint)
    {
        if (_sessions.TryRemove(clientEndpoint, out var client))
        {
            client.Dispose();
        }
    }

    public void Dispose()
    {
        _stopCts.Dispose();
        _localClient?.Dispose();
        foreach (var s in _sessions.Values) s.Dispose();
        _sessions.Clear();
    }
}
