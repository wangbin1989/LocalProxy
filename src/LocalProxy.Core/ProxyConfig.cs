namespace LocalProxy.Core;

public record ProxyConfig
{
    public string Name { get; init; } = string.Empty;
    public ProxyProtocol Protocol { get; init; } = ProxyProtocol.Tcp;
    public int LocalPort { get; init; }
    public string RemoteHost { get; init; } = string.Empty;
    public int RemotePort { get; init; }
    public int MaxConnections { get; init; } = 100;
    public int TimeoutSeconds { get; init; } = 30;
    public bool Enabled { get; init; } = true;
}
