using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

/// <summary>ConfigService 测试：配置文件的加载、添加、更新、删除、启停</summary>
public class ConfigServiceTests
{
    /// <summary>加载含多个代理的合法 JSON 文件，返回正确数量</summary>
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

    /// <summary>加载合法配置后各字段值正确</summary>
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

    /// <summary>protocol 字段大小写不敏感，统一映射为正确枚举</summary>
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

    /// <summary>单个配置的 JSON 文件返回仅含一项的列表</summary>
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

    /// <summary>文件不存在时返回空列表，不抛异常</summary>
    [Fact]
    public async Task LoadAsync_NonExistentFile_ReturnsEmptyList()
    {
        var path = Path.GetTempFileName() + ".json";
        var configs = await ConfigService.LoadAsync(path);
        Assert.Empty(configs);
    }

    /// <summary>添加配置后持久化到文件并可重新加载</summary>
    [Fact]
    public async Task AddAsync_ValidConfig_PersistsToFile()
    {
        var path = WriteTempJson("[]");
        var config = new ProxyConfig
        {
            Name = "new-proxy",
            LocalPort = 8000,
            RemoteHost = "new.example.com",
            RemotePort = 9000,
            Protocol = ProxyProtocol.Tcp
        };

        await ConfigService.AddAsync(path, config);
        var loaded = await ConfigService.LoadAsync(path);

        Assert.Single(loaded);
        Assert.Equal("new-proxy", loaded[0].Name);
        Assert.Equal(8000, loaded[0].LocalPort);
    }

    /// <summary>添加重名配置抛出 InvalidOperationException</summary>
    [Fact]
    public async Task AddAsync_DuplicateName_Throws()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        var dup = new ProxyConfig
        {
            Name = "a",
            LocalPort = 2,
            RemoteHost = "h2",
            RemotePort = 2,
            Protocol = ProxyProtocol.Udp
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfigService.AddAsync(path, dup));
    }

    /// <summary>更新已有配置后字段值正确</summary>
    [Fact]
    public async Task UpdateAsync_ExistingName_UpdatesFields()
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

        await ConfigService.UpdateAsync(path, "a", updated);
        var loaded = await ConfigService.LoadAsync(path);

        Assert.Equal(9999, loaded[0].LocalPort);
        Assert.Equal("updated.example.com", loaded[0].RemoteHost);
        Assert.Equal(8888, loaded[0].RemotePort);
        Assert.Equal(ProxyProtocol.Http, loaded[0].Protocol);
    }

    /// <summary>更新不存在的配置抛出 InvalidOperationException</summary>
    [Fact]
    public async Task UpdateAsync_NotFound_Throws()
    {
        var path = WriteTempJson("[]");
        var updated = new ProxyConfig { Name = "x", LocalPort = 1, RemoteHost = "h", RemotePort = 1 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfigService.UpdateAsync(path, "x", updated));
    }

    /// <summary>删除已有配置后文件内容为空数组</summary>
    [Fact]
    public async Task RemoveAsync_ExistingName_RemovesAndPersists()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        await ConfigService.RemoveAsync(path, "a");
        var loaded = await ConfigService.LoadAsync(path);
        Assert.Empty(loaded);
    }

    /// <summary>删除不存在的配置抛出 InvalidOperationException</summary>
    [Fact]
    public async Task RemoveAsync_NotFound_Throws()
    {
        var path = WriteTempJson("[]");
        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfigService.RemoveAsync(path, "x"));
    }

    /// <summary>先停用再启用，Enabled 状态正确切换</summary>
    [Fact]
    public async Task SetEnabledAsync_DisableThenEnable_UpdatesState()
    {
        var path = WriteTempJson("""
        [{"name":"a","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp"}]
        """);

        await ConfigService.SetEnabledAsync(path, "a", false);
        var afterDisable = await ConfigService.LoadAsync(path);
        Assert.False(afterDisable[0].Enabled);

        await ConfigService.SetEnabledAsync(path, "a", true);
        var afterEnable = await ConfigService.LoadAsync(path);
        Assert.True(afterEnable[0].Enabled);
    }

    /// <summary>启停不存在的配置抛出 InvalidOperationException</summary>
    [Fact]
    public async Task SetEnabledAsync_NotFound_Throws()
    {
        var path = WriteTempJson("[]");
        await Assert.ThrowsAsync<InvalidOperationException>(() => ConfigService.SetEnabledAsync(path, "x", true));
    }

    /// <summary>SaveAsync 自动创建不存在的目标目录</summary>
    [Fact]
    public async Task SaveAsync_CreatesDirectoryIfNeeded()
    {
        var dir = Path.Combine(Path.GetTempPath(), "localproxy_test_" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "config.json");
        var configs = new List<ProxyConfig>
        {
            new() { Name = "a", LocalPort = 1, RemoteHost = "h", RemotePort = 1 }
        };

        await ConfigService.SaveAsync(path, configs);
        Assert.True(File.Exists(path));

        var loaded = await ConfigService.LoadAsync(path);
        Assert.Single(loaded);

        Directory.Delete(dir, true);
    }

    /// <summary>DefaultConfigPath 返回用户目录下的 .localproxy.json</summary>
    [Fact]
    public void DefaultConfigPath_ReturnsPathUnderUserProfile()
    {
        var path = ConfigService.DefaultConfigPath;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.StartsWith(home, path);
        Assert.EndsWith(".localproxy.json", path);
    }

    /// <summary>写入临时 JSON 文件并返回路径</summary>
    private static string WriteTempJson(string content)
    {
        var path = Path.GetTempFileName() + ".json";
        File.WriteAllText(path, content);
        return path;
    }
}
