using LocalProxy.Handlers;
using LocalProxy.Models;

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
}
