using System.CommandLine;
using LocalProxy.Handlers;
using LocalProxy.Services;

namespace LocalProxy.Commands;

/// <summary>Config 子命令注册：add/update/remove/enable/disable/list</summary>
public static class ConfigCommands
{
    /// <summary>构建所有 config 子命令</summary>
    public static IEnumerable<Command> Build()
    {
        yield return BuildListCommand();
        yield return BuildAddCommand();
        yield return BuildUpdateCommand();
        yield return BuildRemoveCommand();
        yield return BuildEnableCommand();
        yield return BuildDisableCommand();
    }

    /// <summary>创建共享的 --config 选项，默认值为 ~/.localproxy.json</summary>
    private static Option<string> ConfigOption() =>
        new("--config") { Description = "配置文件路径", DefaultValueFactory = _ => ConfigService.DefaultConfigPath };

    /// <summary>构建 list 子命令：列出所有代理配置</summary>
    private static Command BuildListCommand()
    {
        var cmd = new Command("list", "列出所有代理配置");
        var configOption = ConfigOption();
        cmd.Add(configOption);

        cmd.SetAction(async parseResult =>
        {
            var file = parseResult.GetValue(configOption)!;
            return await ConfigHandlers.HandleList(file);
        });

        return cmd;
    }

    /// <summary>构建 add 子命令：添加代理配置</summary>
    private static Command BuildAddCommand()
    {
        var cmd = new Command("add", "添加代理配置");

        var configOption = ConfigOption();
        var nameArg = new Argument<string>("name") { Description = "代理名称" };
        var localPortOption = new Option<int>("--local-port", "-l") { Description = "本地监听端口" };
        var remoteHostOption = new Option<string>("--remote-host", "-H") { Description = "远程目标主机" };
        var remotePortOption = new Option<int>("--remote-port", "-p") { Description = "远程目标端口" };
        var protocolOption = new Option<string>("--protocol", "-P") { Description = "代理协议 (tcp, udp, http)", DefaultValueFactory = _ => "tcp" };

        cmd.Add(configOption);
        cmd.Add(nameArg);
        cmd.Add(localPortOption);
        cmd.Add(remoteHostOption);
        cmd.Add(remotePortOption);
        cmd.Add(protocolOption);

        cmd.SetAction(async parseResult =>
        {
            var file = parseResult.GetValue(configOption)!;
            var name = parseResult.GetValue(nameArg)!;
            var localPort = parseResult.GetValue(localPortOption);
            var remoteHost = parseResult.GetValue(remoteHostOption);
            var remotePort = parseResult.GetValue(remotePortOption);
            var protocol = parseResult.GetValue(protocolOption)!;
            // DefaultValueFactory 保证 protocol 非 null

            var config = new ProxyConfig
            {
                Name = name,
                LocalPort = localPort,
                RemoteHost = remoteHost!,
                RemotePort = remotePort,
                Protocol = Enum.Parse<Models.ProxyProtocol>(protocol, ignoreCase: true)
            };

            return await ConfigHandlers.HandleAdd(file, config);
        });

        return cmd;
    }

    /// <summary>构建 update 子命令：更新代理配置</summary>
    private static Command BuildUpdateCommand()
    {
        var cmd = new Command("update", "更新代理配置");

        var configOption = ConfigOption();
        var nameArg = new Argument<string>("name") { Description = "代理名称" };
        var localPortOption = new Option<int?>("--local-port", "-l") { Description = "本地监听端口" };
        var remoteHostOption = new Option<string?>("--remote-host", "-H") { Description = "远程目标主机" };
        var remotePortOption = new Option<int?>("--remote-port", "-p") { Description = "远程目标端口" };
        var protocolOption = new Option<string?>("--protocol", "-P") { Description = "代理协议 (tcp, udp, http)" };

        cmd.Add(configOption);
        cmd.Add(nameArg);
        cmd.Add(localPortOption);
        cmd.Add(remoteHostOption);
        cmd.Add(remotePortOption);
        cmd.Add(protocolOption);

        cmd.SetAction(async parseResult =>
        {
            var file = parseResult.GetValue(configOption)!;
            var name = parseResult.GetValue(nameArg)!;
            var cliLocalPort = parseResult.GetValue(localPortOption);
            var cliRemoteHost = parseResult.GetValue(remoteHostOption);
            var cliRemotePort = parseResult.GetValue(remotePortOption);
            var cliProtocol = parseResult.GetValue(protocolOption);

            var configs = await ConfigService.LoadAsync(file);
            var existing = configs.Find(c =>
                string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                Infrastructure.ConsoleOutput.Error($"代理 '{name}' 不存在");
                return 1;
            }

            var updated = new ProxyConfig
            {
                Name = existing.Name,
                LocalPort = cliLocalPort ?? existing.LocalPort,
                RemoteHost = cliRemoteHost ?? existing.RemoteHost,
                RemotePort = cliRemotePort ?? existing.RemotePort,
                Protocol = cliProtocol is not null
                    ? Enum.Parse<Models.ProxyProtocol>(cliProtocol, ignoreCase: true)
                    : existing.Protocol,
                Enabled = existing.Enabled
            };

            return await ConfigHandlers.HandleUpdate(file, name, updated);
        });

        return cmd;
    }

    /// <summary>构建 remove 子命令：删除代理配置</summary>
    private static Command BuildRemoveCommand()
    {
        var cmd = new Command("remove", "删除代理配置");

        var configOption = ConfigOption();
        var nameArg = new Argument<string>("name") { Description = "代理名称" };
        var forceOption = new Option<bool>("--force") { Description = "跳过确认" };

        cmd.Add(configOption);
        cmd.Add(nameArg);
        cmd.Add(forceOption);

        cmd.SetAction(async parseResult =>
        {
            var file = parseResult.GetValue(configOption)!;
            var name = parseResult.GetValue(nameArg)!;
            var force = parseResult.GetValue(forceOption);

            return await ConfigHandlers.HandleRemove(file, name, force);
        });

        return cmd;
    }

    /// <summary>构建 enable 子命令：启用代理</summary>
    private static Command BuildEnableCommand()
    {
        var cmd = new Command("enable", "启用代理");

        var configOption = ConfigOption();
        var nameArg = new Argument<string>("name") { Description = "代理名称" };

        cmd.Add(configOption);
        cmd.Add(nameArg);

        cmd.SetAction(async parseResult =>
        {
            var file = parseResult.GetValue(configOption)!;
            var name = parseResult.GetValue(nameArg)!;
            return await ConfigHandlers.HandleEnable(file, name);
        });

        return cmd;
    }

    /// <summary>构建 disable 子命令：停用代理</summary>
    private static Command BuildDisableCommand()
    {
        var cmd = new Command("disable", "停用代理");

        var configOption = ConfigOption();
        var nameArg = new Argument<string>("name") { Description = "代理名称" };

        cmd.Add(configOption);
        cmd.Add(nameArg);

        cmd.SetAction(async parseResult =>
        {
            var file = parseResult.GetValue(configOption)!;
            var name = parseResult.GetValue(nameArg)!;
            return await ConfigHandlers.HandleDisable(file, name);
        });

        return cmd;
    }
}
