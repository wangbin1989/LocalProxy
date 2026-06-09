using System.Collections.Immutable;
using LocalProxy.Core;

namespace LocalProxy.Config;

public static class PresetProfiles
{
    private static readonly ImmutableDictionary<string, PresetProfile> Profiles = CreatePresets();

    public static IReadOnlyList<PresetProfile> List() => [.. Profiles.Values];

    public static PresetProfile? Get(string name) =>
        Profiles.TryGetValue(name, out var profile) ? profile : null;

    private static ImmutableDictionary<string, PresetProfile> CreatePresets()
    {
        var presets = new Dictionary<string, PresetProfile>
        {
            ["SQL Server"] = new("SQL Server", ProxyProtocol.Tcp, 1433, "Database"),
            ["Redis"] = new("Redis", ProxyProtocol.Tcp, 6379, "Cache"),
            ["MySQL"] = new("MySQL", ProxyProtocol.Tcp, 3306, "Database"),
            ["PostgreSQL"] = new("PostgreSQL", ProxyProtocol.Tcp, 5432, "Database"),
            ["MongoDB"] = new("MongoDB", ProxyProtocol.Tcp, 27017, "Database"),
            ["RabbitMQ"] = new("RabbitMQ", ProxyProtocol.Tcp, 5672, "Message Queue"),
            ["Elasticsearch"] = new("Elasticsearch", ProxyProtocol.Tcp, 9200, "Search"),
            ["Kafka"] = new("Kafka", ProxyProtocol.Tcp, 9092, "Message Broker"),
        };
        return presets.ToImmutableDictionary();
    }
}

public record PresetProfile(string Name, ProxyProtocol Protocol, int DefaultPort, string Category)
{
    public ProxyConfig ToConfig(string remoteHost, int localPort, int? remotePort = null) => new()
    {
        Name = Name,
        Protocol = Protocol,
        LocalPort = localPort,
        RemoteHost = remoteHost,
        RemotePort = remotePort ?? DefaultPort,
    };
}
