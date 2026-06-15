using LocalProxy.Handlers;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

public class RunHandlerTests
{
    [Fact]
    public async Task Handle_InvalidLocalPort_ReturnsError()
    {
        var result = await RunHandler.Handle(-1, "example.com", 80, ProxyProtocol.Tcp);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Handle_LocalPortOutOfRange_ReturnsError()
    {
        var result = await RunHandler.Handle(65536, "example.com", 80, ProxyProtocol.Tcp);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Handle_InvalidRemotePort_ReturnsError()
    {
        var result = await RunHandler.Handle(8080, "example.com", -1, ProxyProtocol.Tcp);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Handle_RemotePortOutOfRange_ReturnsError()
    {
        var result = await RunHandler.Handle(8080, "example.com", 65536, ProxyProtocol.Tcp);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Handle_EmptyRemoteHost_ReturnsError()
    {
        var result = await RunHandler.Handle(8080, "", 80, ProxyProtocol.Tcp);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Handle_WhitespaceRemoteHost_ReturnsError()
    {
        var result = await RunHandler.Handle(8080, "   ", 80, ProxyProtocol.Tcp);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Handle_MultipleErrors_ReturnsAllErrors()
    {
        var result = await RunHandler.Handle(-1, "", 99999, ProxyProtocol.Tcp);
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
        var configs = new List<ProxyConfig>();

        var result = await RunHandler.HandleMultiple(configs);
        Assert.Equal(0, result);
    }
}
