namespace LocalProxy.Core;

public record TunnelStats
{
    public long BytesIn { get; init; }
    public long BytesOut { get; init; }
    public int ActiveConnections { get; init; }
    public TimeSpan Uptime { get; init; }
    public TunnelStatus Status { get; init; }
}
