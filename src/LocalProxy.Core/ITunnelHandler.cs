namespace LocalProxy.Core;

public interface ITunnelHandler
{
    string Name { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    TunnelStats GetStats();
}

public enum TunnelStatus
{
    Stopped,
    Starting,
    Running,
    Degraded,
    Stopping,
    Error
}

public enum ProxyProtocol
{
    Tcp,
    Udp,
    Http
}
