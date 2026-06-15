using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

public class ConfigServiceTests
{
    [Fact]
    public async Task LoadAsync_ValidMultiProxyConfig_ReturnsCorrectCount()
    {
        var path = WriteTempJson("""
        [
          {
            "name": "proxy1",
            "localPort": 8080,
            "remoteHost": "host1.example.com",
            "remotePort": 80,
            "protocol": "tcp"
          },
          {
            "name": "proxy2",
            "localPort": 9090,
            "remoteHost": "host2.example.com",
            "remotePort": 443,
            "protocol": "http"
          }
        ]
        """);

        var configs = await ConfigService.LoadAsync(path);
        Assert.Equal(2, configs.Count);
    }

    [Fact]
    public async Task LoadAsync_ValidConfig_ReturnsCorrectFields()
    {
        var path = WriteTempJson("""
        [
          {
            "name": "test-proxy",
            "localPort": 1234,
            "remoteHost": "test.example.com",
            "remotePort": 5678,
            "protocol": "udp"
          }
        ]
        """);

        var configs = await ConfigService.LoadAsync(path);
        var c = configs[0];

        Assert.Equal("test-proxy", c.Name);
        Assert.Equal(1234, c.LocalPort);
        Assert.Equal("test.example.com", c.RemoteHost);
        Assert.Equal(5678, c.RemotePort);
        Assert.Equal(ProxyProtocol.Udp, c.Protocol);
    }

    [Theory]
    [InlineData("tcp", ProxyProtocol.Tcp)]
    [InlineData("TCP", ProxyProtocol.Tcp)]
    [InlineData("udp", ProxyProtocol.Udp)]
    [InlineData("UDP", ProxyProtocol.Udp)]
    [InlineData("http", ProxyProtocol.Http)]
    [InlineData("HTTP", ProxyProtocol.Http)]
    public async Task LoadAsync_ProtocolCaseInsensitive_ReturnsCorrectEnum(string input, ProxyProtocol expected)
    {
        var json = $$"""
        [
          {
            "name": "p",
            "localPort": 1,
            "remoteHost": "h",
            "remotePort": 1,
            "protocol": "{{input}}"
          }
        ]
        """;
        var path = WriteTempJson(json);

        var configs = await ConfigService.LoadAsync(path);
        Assert.Equal(expected, configs[0].Protocol);
    }

    [Fact]
    public async Task LoadAsync_SingleConfig_ReturnsListWithOneItem()
    {
        var path = WriteTempJson("""
        [
          {
            "name": "solo",
            "localPort": 3000,
            "remoteHost": "solo.example.com",
            "remotePort": 4000,
            "protocol": "tcp"
          }
        ]
        """);

        var configs = await ConfigService.LoadAsync(path);
        Assert.Single(configs);
    }

    private static string WriteTempJson(string content)
    {
        var path = Path.GetTempFileName() + ".json";
        File.WriteAllText(path, content);
        return path;
    }
}
