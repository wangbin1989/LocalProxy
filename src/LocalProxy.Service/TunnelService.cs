using System.Text.Json;
using LocalProxy.Config;
using LocalProxy.Core;
using LocalProxy.Engine;
using LocalProxy.Ipc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalProxy.Service;

public sealed class TunnelService : IHostedService, IDisposable
{
    private readonly ConfigManager _configManager;
    private readonly ILogger<TunnelService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, TunnelEntry> _tunnels = new();
    private IpcServer? _ipcServer;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;

    public TunnelService(ConfigManager configManager, ILogger<TunnelService> logger, ILoggerFactory loggerFactory)
    {
        _configManager = configManager;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _logger.LogInformation("LocalProxy service starting...");

        var configs = _configManager.Load();
        foreach (var config in configs.Where(c => c.Enabled))
        {
            await StartTunnelAsync(config);
        }

        // Start IPC server
        _ipcServer = new IpcServer(IpcProtocol.PipeName, HandleIpcRequest);
        _ = _ipcServer.StartAsync(_cts.Token);

        // Watch for config changes
        _configManager.ConfigChanged += OnConfigChanged;
        _configManager.StartWatching();

        _logger.LogInformation("LocalProxy service started with {Count} tunnel(s)", _tunnels.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("LocalProxy service stopping...");

        _configManager.StopWatching();
        _configManager.ConfigChanged -= OnConfigChanged;
        _ipcServer?.Stop();

        _cts?.Cancel();

        var stopTasks = new List<Task>();
        lock (_lock)
        {
            foreach (var entry in _tunnels.Values)
            {
                entry.Cts.Cancel();
                stopTasks.Add(entry.Handler.StopAsync(cancellationToken));
            }
        }

        await Task.WhenAll(stopTasks);

        lock (_lock)
        {
            foreach (var entry in _tunnels.Values)
                entry.Dispose();
            _tunnels.Clear();
        }

        _logger.LogInformation("LocalProxy service stopped");
    }

    private async Task StartTunnelAsync(ProxyConfig config)
    {
        lock (_lock)
        {
            if (_tunnels.ContainsKey(config.Name))
                return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts?.Token ?? default);
        var handler = CreateHandler(config);

        lock (_lock)
        {
            _tunnels[config.Name] = new TunnelEntry(handler, cts);
        }

        // Start handler in background with auto-restart
        _ = RunTunnelAsync(config.Name, handler, cts.Token);

        _logger.LogInformation("Tunnel '{Name}' ({Protocol}) initialized on port {Port}",
            config.Name, config.Protocol, config.LocalPort);

        await Task.CompletedTask;
    }

    private async Task RunTunnelAsync(string name, ITunnelHandler handler, CancellationToken ct)
    {
        var retryDelay = 1;
        const int maxRetryDelay = 60;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await handler.StartAsync(ct);
                retryDelay = 1; // Reset on clean exit
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Tunnel '{Name}' failed, restarting in {Delay}s", name, retryDelay);

                try { await Task.Delay(TimeSpan.FromSeconds(retryDelay), ct); } catch (OperationCanceledException) { break; }

                retryDelay = Math.Min(retryDelay * 2, maxRetryDelay);
            }
        }
    }

    private ITunnelHandler CreateHandler(ProxyConfig config)
    {
        return config.Protocol switch
        {
            ProxyProtocol.Tcp => new TcpForwardHandler(config, _loggerFactory.CreateLogger<TcpForwardHandler>()),
            ProxyProtocol.Udp => new UdpForwardHandler(config, _loggerFactory.CreateLogger<UdpForwardHandler>()),
            ProxyProtocol.Http => new HttpForwardHandler(config, _loggerFactory.CreateLogger<HttpForwardHandler>()),
            _ => throw new ConfigException($"Unsupported protocol: {config.Protocol}")
        };
    }

    private async Task<JsonRpcResponse> HandleIpcRequest(JsonRpcRequest request, CancellationToken ct)
    {
        return request.Method switch
        {
            IpcProtocol.MethodListTunnels => HandleListTunnels(request),
            IpcProtocol.MethodStartTunnel => await HandleStartTunnel(request),
            IpcProtocol.MethodStopTunnel => await HandleStopTunnel(request),
            IpcProtocol.MethodGetStats => HandleGetStats(request),
            _ => new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = -32601, Message = $"Method not found: {request.Method}" }
            }
        };
    }

    private JsonRpcResponse HandleListTunnels(JsonRpcRequest request)
    {
        lock (_lock)
        {
            var tunnelInfos = _tunnels.Select(kv =>
            {
                var stats = kv.Value.Handler.GetStats();
                return new
                {
                    name = kv.Key,
                    status = stats.Status.ToString().ToLowerInvariant(),
                    stats.BytesIn,
                    stats.BytesOut,
                    stats.ActiveConnections,
                    Uptime = stats.Uptime.ToString()
                };
            }).ToArray();

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(tunnelInfos)
            };
        }
    }

    private async Task<JsonRpcResponse> HandleStartTunnel(JsonRpcRequest request)
    {
        var name = request.Params?.GetProperty("name").GetString();
        if (string.IsNullOrEmpty(name))
        {
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32602, Message = "Missing 'name' parameter" } };
        }

        var configs = _configManager.Load();
        var config = configs.FirstOrDefault(c => c.Name == name);
        if (config == null)
        {
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = IpcProtocol.ErrorTunnelNotFound, Message = $"Tunnel '{name}' not found" } };
        }

        lock (_lock)
        {
            if (_tunnels.TryGetValue(name, out var entry))
            {
                var stats = entry.Handler.GetStats();
                if (stats.Status == TunnelStatus.Running)
                {
                    return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = IpcProtocol.ErrorTunnelAlreadyRunning, Message = $"Tunnel '{name}' is already running" } };
                }
            }
        }

        await StartTunnelAsync(config);
        return new JsonRpcResponse { Id = request.Id, Result = JsonSerializer.SerializeToElement(new { status = "started" }) };
    }

    private async Task<JsonRpcResponse> HandleStopTunnel(JsonRpcRequest request)
    {
        var name = request.Params?.GetProperty("name").GetString();
        if (string.IsNullOrEmpty(name))
        {
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32602, Message = "Missing 'name' parameter" } };
        }

        TunnelEntry? entry;
        lock (_lock)
        {
            if (!_tunnels.TryGetValue(name, out entry))
            {
                return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = IpcProtocol.ErrorTunnelNotFound, Message = $"Tunnel '{name}' not found" } };
            }
        }

        entry.Cts.Cancel();
        await entry.Handler.StopAsync();

        lock (_lock)
        {
            _tunnels.Remove(name);
            entry.Dispose();
        }

        return new JsonRpcResponse { Id = request.Id, Result = JsonSerializer.SerializeToElement(new { status = "stopped" }) };
    }

    private JsonRpcResponse HandleGetStats(JsonRpcRequest request)
    {
        var name = request.Params?.GetProperty("name").GetString();
        if (string.IsNullOrEmpty(name))
        {
            return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = -32602, Message = "Missing 'name' parameter" } };
        }

        lock (_lock)
        {
            if (!_tunnels.TryGetValue(name, out var entry))
            {
                return new JsonRpcResponse { Id = request.Id, Error = new JsonRpcError { Code = IpcProtocol.ErrorTunnelNotFound, Message = $"Tunnel '{name}' not found" } };
            }

            var stats = entry.Handler.GetStats();
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = JsonSerializer.SerializeToElement(new
                {
                    name,
                    status = stats.Status.ToString().ToLowerInvariant(),
                    stats.BytesIn,
                    stats.BytesOut,
                    stats.ActiveConnections,
                    Uptime = stats.Uptime.ToString()
                })
            };
        }
    }

    private async void OnConfigChanged(object? sender, IReadOnlyList<ProxyConfig> configs)
    {
        _logger.LogInformation("Config changed, reloading tunnels...");

        // Stop removed tunnels
        var configNames = configs.Select(c => c.Name).ToHashSet();
        lock (_lock)
        {
            var toRemove = _tunnels.Keys.Where(k => !configNames.Contains(k)).ToList();
            foreach (var name in toRemove)
            {
                if (_tunnels.TryGetValue(name, out var entry))
                {
                    entry.Cts.Cancel();
                    _ = entry.Handler.StopAsync();
                    _tunnels.Remove(name);
                    entry.Dispose();
                    _logger.LogInformation("Tunnel '{Name}' removed (config deleted)", name);
                }
            }
        }

        // Start new or changed tunnels
        foreach (var config in configs.Where(c => c.Enabled))
        {
            lock (_lock)
            {
                if (_tunnels.TryGetValue(config.Name, out var existing))
                {
                    var stats = existing.Handler.GetStats();
                    if (stats.Status == TunnelStatus.Running)
                        continue; // Already running, leave it alone

                    // Restart
                    existing.Cts.Cancel();
                    _ = existing.Handler.StopAsync();
                    existing.Dispose();
                    _tunnels.Remove(config.Name);
                }
            }

            await StartTunnelAsync(config);
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _ipcServer?.Dispose();
        lock (_lock)
        {
            foreach (var entry in _tunnels.Values)
                entry.Dispose();
            _tunnels.Clear();
        }
    }
}

internal sealed class TunnelEntry : IDisposable
{
    public ITunnelHandler Handler { get; }
    public CancellationTokenSource Cts { get; }

    public TunnelEntry(ITunnelHandler handler, CancellationTokenSource cts)
    {
        Handler = handler;
        Cts = cts;
    }

    public void Dispose()
    {
        Cts.Dispose();
        Handler.Dispose();
    }
}
