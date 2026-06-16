using LocalProxy.Handlers;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

/// <summary>Config 命令处理程序测试：add / update / remove / enable / disable / list</summary>
public class ConfigHandlersTests
{
    /// <summary>添加有效配置返回 0，并持久化到文件</summary>
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

    /// <summary>添加重名配置返回错误码 1</summary>
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

    /// <summary>未指定 Protocol 时默认为 Tcp</summary>
    [Fact]
    public async Task HandleAdd_DefaultProtocol_DefaultsToTcp()
    {
        var path = WriteTempJson("[]");
        var config = new ProxyConfig
        {
            Name = "default-proto",
            LocalPort = 8000,
            RemoteHost = "example.com",
            RemotePort = 9000
        };

        var result = await ConfigHandlers.HandleAdd(path, config);
        Assert.Equal(0, result);

        var loaded = await ConfigService.LoadAsync(path);
        Assert.Single(loaded);
        Assert.Equal(ProxyProtocol.Tcp, loaded[0].Protocol);
    }

    /// <summary>强制删除已有配置返回 0，文件内容为空数组</summary>
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

    /// <summary>删除不存在的配置返回错误码 1</summary>
    [Fact]
    public async Task HandleRemove_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var result = await ConfigHandlers.HandleRemove(path, "x", force: true);
        Assert.Equal(1, result);
    }

    /// <summary>更新已存在的配置返回 0</summary>
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

    /// <summary>更新不存在的配置返回错误码 1</summary>
    [Fact]
    public async Task HandleUpdate_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var updated = new ProxyConfig { Name = "x", LocalPort = 1, RemoteHost = "h", RemotePort = 1 };

        var result = await ConfigHandlers.HandleUpdate(path, "x", updated);
        Assert.Equal(1, result);
    }

    /// <summary>启用已有配置返回 0</summary>
    [Fact]
    public async Task HandleEnable_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleEnable(path, "a");
        Assert.Equal(0, result);
    }

    /// <summary>启用不存在的配置返回错误码 1</summary>
    [Fact]
    public async Task HandleEnable_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var result = await ConfigHandlers.HandleEnable(path, "x");
        Assert.Equal(1, result);
    }

    /// <summary>停用已有配置返回 0</summary>
    [Fact]
    public async Task HandleDisable_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleDisable(path, "a");
        Assert.Equal(0, result);
    }

    /// <summary>停用不存在的配置返回错误码 1</summary>
    [Fact]
    public async Task HandleDisable_NotFound_ReturnsError()
    {
        var path = WriteTempJson("[]");
        var result = await ConfigHandlers.HandleDisable(path, "x");
        Assert.Equal(1, result);
    }

    /// <summary>列出配置返回 0</summary>
    [Fact]
    public async Task HandleList_ReturnsSuccess()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var result = await ConfigHandlers.HandleList(path);
        Assert.Equal(0, result);
    }

    /// <summary>写入临时 JSON 文件并返回路径</summary>
    private static string WriteTempJson(string content)
    {
        var path = Path.GetTempFileName() + ".json";
        File.WriteAllText(path, content);
        return path;
    }
}
