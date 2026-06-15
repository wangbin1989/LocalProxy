using LocalProxy.Handlers;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

public class RunHandlerTests
{
    private static List<ProxyConfig> Single(string name, int localPort, string remoteHost, int remotePort) =>
    [
        new() { Name = name, LocalPort = localPort, RemoteHost = remoteHost, RemotePort = remotePort }
    ];

    [Fact]
    public async Task HandleMultiple_InvalidLocalPort_ReturnsError()
    {
        var result = await RunHandler.HandleMultiple(Single("a", -1, "example.com", 80));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_LocalPortOutOfRange_ReturnsError()
    {
        var result = await RunHandler.HandleMultiple(Single("a", 65536, "example.com", 80));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_InvalidRemotePort_ReturnsError()
    {
        var result = await RunHandler.HandleMultiple(Single("a", 8080, "example.com", -1));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_RemotePortOutOfRange_ReturnsError()
    {
        var result = await RunHandler.HandleMultiple(Single("a", 8080, "example.com", 65536));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_EmptyRemoteHost_ReturnsError()
    {
        var result = await RunHandler.HandleMultiple(Single("a", 8080, "", 80));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_WhitespaceRemoteHost_ReturnsError()
    {
        var result = await RunHandler.HandleMultiple(Single("a", 8080, "   ", 80));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_MultipleErrors_ReturnsAllErrors()
    {
        var result = await RunHandler.HandleMultiple(Single("a", -1, "", 99999));
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_AllConfigsInvalid_ReturnsError()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "a", LocalPort = -1, RemoteHost = "h", RemotePort = 1 },
            new() { Name = "b", LocalPort = 1, RemoteHost = "", RemotePort = 1 },
            new() { Name = "c", LocalPort = 1, RemoteHost = "h", RemotePort = 99999 },
        };

        var result = await RunHandler.HandleMultiple(configs);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_OneConfigInvalid_ReturnsError()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "valid", LocalPort = 8080, RemoteHost = "example.com", RemotePort = 80 },
            new() { Name = "invalid", LocalPort = -1, RemoteHost = "h", RemotePort = 1 },
        };

        var result = await RunHandler.HandleMultiple(configs);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleMultiple_EmptyList_ReturnsSuccess()
    {
        var result = await RunHandler.HandleMultiple([]);
        Assert.Equal(0, result);
    }
}
