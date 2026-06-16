using System.CommandLine;
using LocalProxy.Commands;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Tests;

/// <summary>集成测试：CLI 命令解析 + Handler 端到端代理流程</summary>
public class IntegrationTests
{
    /// <summary>解析命令行并执行，返回退出码</summary>
    private static async Task<int> InvokeAsync(Command root, string args)
    {
        var parseResult = root.Parse(args);
        return await parseResult.InvokeAsync();
    }

    /// <summary>构建完整命令树</summary>
    private static Command BuildRootCommand()
    {
        var root = new RootCommand("LocalProxy - 本地端口代理转发工具");
        root.Add(RunCommand.Build());
        foreach (var cmd in ConfigCommands.Build())
            root.Add(cmd);
        return root;
    }

    // ── CLI 命令集成测试 ──

    /// <summary>run --config 指向不存在的文件，返回 0</summary>
    [Fact]
    public async Task RunCommand_NoConfigFile_ReturnsSuccess()
    {
        var root = BuildRootCommand();
        var path = Path.GetTempFileName() + ".json";

        var exitCode = await InvokeAsync(root, $"run --config {path}");
        Assert.Equal(0, exitCode);
    }

    /// <summary>run：空配置列表返回 0</summary>
    [Fact]
    public async Task RunCommand_EmptyConfig_ReturnsSuccess()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("[]");

        var exitCode = await InvokeAsync(root, $"run --config {path}");
        Assert.Equal(0, exitCode);
    }

    /// <summary>run：无效端口返回错误码 1</summary>
    [Fact]
    public async Task RunCommand_InvalidPort_ReturnsError()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("""
        [{"name":"bad","localPort":-1,"remoteHost":"h","remotePort":80,"protocol":"tcp","enabled":true}]
        """);

        var exitCode = await InvokeAsync(root, $"run --config {path}");
        Assert.Equal(1, exitCode);
    }

    /// <summary>list：空配置文件列出不报错</summary>
    [Fact]
    public async Task ListCommand_EmptyConfig_ReturnsSuccess()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("[]");

        var exitCode = await InvokeAsync(root, $"list --config {path}");
        Assert.Equal(0, exitCode);
    }

    /// <summary>add 后 list 可见，配置持久化</summary>
    [Fact]
    public async Task AddThenList_PersistsAndShows()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("[]");

        await InvokeAsync(root, $"add my-add -l 8001 -H example.com -p 80 --protocol tcp --config {path}");
        var exitCode = await InvokeAsync(root, $"list --config {path}");
        Assert.Equal(0, exitCode);

        var configs = await ConfigService.LoadAsync(path);
        Assert.Single(configs);
        Assert.Equal("my-add", configs[0].Name);
        Assert.Equal(8001, configs[0].LocalPort);
    }

    /// <summary>add：省略 --protocol 默认 tcp</summary>
    [Fact]
    public async Task AddCommand_DefaultProtocol_UsesTcp()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("[]");

        await InvokeAsync(root, $"add default-proto -l 9001 -H host.com -p 443 --config {path}");

        var configs = await ConfigService.LoadAsync(path);
        Assert.Equal(ProxyProtocol.Tcp, configs[0].Protocol);
    }

    /// <summary>add：重名返回错误码 1</summary>
    [Fact]
    public async Task AddCommand_DuplicateName_ReturnsError()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("""
        [{"name":"dup","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp","enabled":true}]
        """);

        var exitCode = await InvokeAsync(root, $"add dup -l 2 -H h2 -p 2 --config {path}");
        Assert.Equal(1, exitCode);
    }

    /// <summary>remove --force 删除已有配置</summary>
    [Fact]
    public async Task RemoveCommand_ExistingConfig_Removes()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("""
        [{"name":"to-remove","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp","enabled":true}]
        """);

        await InvokeAsync(root, $"remove to-remove --force --config {path}");

        var configs = await ConfigService.LoadAsync(path);
        Assert.Empty(configs);
    }

    /// <summary>enable / disable 切换状态</summary>
    [Fact]
    public async Task EnableThenDisable_TogglesState()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("""
        [{"name":"toggle","localPort":1,"remoteHost":"h","remotePort":1,"protocol":"tcp","enabled":false}]
        """);

        await InvokeAsync(root, $"enable toggle --config {path}");
        var afterEnable = await ConfigService.LoadAsync(path);
        Assert.True(afterEnable[0].Enabled);

        await InvokeAsync(root, $"disable toggle --config {path}");
        var afterDisable = await ConfigService.LoadAsync(path);
        Assert.False(afterDisable[0].Enabled);
    }

    /// <summary>update 更新字段</summary>
    [Fact]
    public async Task UpdateCommand_ExistingConfig_UpdatesFields()
    {
        var root = BuildRootCommand();
        var path = WriteTempJson("""
        [{"name":"upd","localPort":1,"remoteHost":"old","remotePort":1,"protocol":"tcp","enabled":true}]
        """);

        await InvokeAsync(root, $"update upd -l 9999 -H new-host.com -p 8888 --protocol http --config {path}");

        var configs = await ConfigService.LoadAsync(path);
        Assert.Equal(9999, configs[0].LocalPort);
        Assert.Equal("new-host.com", configs[0].RemoteHost);
        Assert.Equal(8888, configs[0].RemotePort);
        Assert.Equal(ProxyProtocol.Http, configs[0].Protocol);
    }

    // ── helper ──

    private static string WriteTempJson(string content)
    {
        var path = Path.GetTempFileName() + ".json";
        File.WriteAllText(path, content);
        return path;
    }
}
