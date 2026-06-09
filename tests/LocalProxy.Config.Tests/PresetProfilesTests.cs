using LocalProxy.Core;

namespace LocalProxy.Config.Tests;

public class PresetProfilesTests
{
    [Fact]
    public void AllEightPresetsAreAvailable()
    {
        var presets = PresetProfiles.List();
        Assert.Equal(8, presets.Count);
    }

    [Fact]
    public void GetKnownPresetReturnsCorrectData()
    {
        var redis = PresetProfiles.Get("Redis");
        Assert.NotNull(redis);
        Assert.Equal("Redis", redis.Name);
        Assert.Equal(ProxyProtocol.Tcp, redis.Protocol);
        Assert.Equal(6379, redis.DefaultPort);
    }

    [Fact]
    public void GetUnknownPresetReturnsNull()
    {
        var unknown = PresetProfiles.Get("UnknownService");
        Assert.Null(unknown);
    }

    [Theory]
    [InlineData("SQL Server", 1433)]
    [InlineData("Redis", 6379)]
    [InlineData("MySQL", 3306)]
    [InlineData("PostgreSQL", 5432)]
    [InlineData("MongoDB", 27017)]
    [InlineData("RabbitMQ", 5672)]
    [InlineData("Elasticsearch", 9200)]
    [InlineData("Kafka", 9092)]
    public void AllPresetsHaveCorrectDefaultPorts(string name, int expectedPort)
    {
        var preset = PresetProfiles.Get(name);
        Assert.NotNull(preset);
        Assert.Equal(expectedPort, preset.DefaultPort);
    }

    [Fact]
    public void PresetToConfigCreatesValidConfig()
    {
        var redis = PresetProfiles.Get("Redis")!;
        var config = redis.ToConfig("redis.example.com", 6379);

        Assert.Equal("Redis", config.Name);
        Assert.Equal(ProxyProtocol.Tcp, config.Protocol);
        Assert.Equal(6379, config.LocalPort);
        Assert.Equal("redis.example.com", config.RemoteHost);
        Assert.Equal(6379, config.RemotePort);
    }
}
