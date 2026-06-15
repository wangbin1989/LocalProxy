using LocalProxy.Handlers;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

public class ConfigHandlersTests
{
    [Fact]
    public async Task HandleAdd_ValidConfig_ReturnsSuccess()
    {
        var path = WriteTempJson("[]");
        var config = new ProxyConfig
        {
            Name = "test-proxy",
            LocalPort = 8000,
            RemoteHost = "test.example.com",
            RemotePort = 9000,
            Protocol = ProxyProtocol.Tcp
        };

        var result = await ConfigHandlers.HandleAdd(path, config);
        Assert.Equal(0, result);

        var loaded = await ConfigService.LoadAsync(path);
        Assert.Single(loaded);
    }

    [Fact]
    public async Task HandleAdd_DuplicateName_ReturnsError()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var dup = new ProxyConfig
        {
            Name = "a",
            LocalPort = 2,
            RemoteHost = "h2",
            RemotePort = 2
        };

        var result = await ConfigHandlers.HandleAdd(path, dup);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleRemove_WithForce_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleRemove(path, "a", force: true);
        Assert.Equal(0, result);

        var loaded = await ConfigService.LoadAsync(path);
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task HandleRemove_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var result = await ConfigHandlers.HandleRemove(path, "x", force: true);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleUpdate_ValidUpdate_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var updated = new ProxyConfig
        {
            Name = "a",
            LocalPort = 9999,
            RemoteHost = "updated.example.com",
            RemotePort = 8888,
            Protocol = ProxyProtocol.Http
        };

        var result = await ConfigHandlers.HandleUpdate(path, "a", updated);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task HandleUpdate_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var updated = new ProxyConfig { Name = "x", LocalPort = 1, RemoteHost = "h", RemotePort = 1 };

        var result = await ConfigHandlers.HandleUpdate(path, "x", updated);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleEnable_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleEnable(path, "a");
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task HandleEnable_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var result = await ConfigHandlers.HandleEnable(path, "x");
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleDisable_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleDisable(path, "a");
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task HandleDisable_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var result = await ConfigHandlers.HandleDisable(path, "x");
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task HandleList_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleList(path);
        Assert.Equal(0, result);
    }

    private static string WriteTempJson(string content)
    {
        var path = Path.GetTempFileName() + ".json";
        File.WriteAllText(path, content);
        return path;
    }
}
