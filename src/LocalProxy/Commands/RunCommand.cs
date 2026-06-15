using System.CommandLine;
using LocalProxy.Handlers;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Commands;

public static class RunCommand
{
    public static Command Build()
    {
        var cmd = new Command("run", "启动代理隧道");

        var configOption = new Option<string?>("--config")
            { Description = "配置文件路径 (JSON 数组)" };

        var localPortOption = new Option<int?>("--local-port", "-l")
            { Description = "本地监听端口 (0-65535)" };
        var remoteHostOption = new Option<string?>("--remote-host", "-H")
            { Description = "远程目标主机地址" };
        var remotePortOption = new Option<int?>("--remote-port", "-p")
            { Description = "远程目标端口 (0-65535)" };
        var protocolOption = new Option<string?>("--protocol", "-P")
            { Description = "代理协议类型 (tcp, udp, http)" };

        cmd.Add(configOption);
        cmd.Add(localPortOption);
        cmd.Add(remoteHostOption);
        cmd.Add(remotePortOption);
        cmd.Add(protocolOption);

        cmd.SetAction(async parseResult =>
        {
            var configPath = parseResult.GetValue(configOption);

            // 从配置文件启动多个代理
            if (configPath is not null)
            {
                var configs = await ConfigService.LoadAsync(configPath);
                return await RunHandler.HandleMultiple(configs);
            }

            // 单代理 CLI 模式
            var cliLocalPort = parseResult.GetValue(localPortOption);
            var cliRemoteHost = parseResult.GetValue(remoteHostOption);
            var cliRemotePort = parseResult.GetValue(remotePortOption);
            var cliProtocol = parseResult.GetValue(protocolOption);

            var localPort = cliLocalPort ?? 0;
            var remoteHost = cliRemoteHost!;
            var remotePort = cliRemotePort ?? 0;
            var protocol = cliProtocol is not null
                ? Enum.Parse<ProxyProtocol>(cliProtocol, ignoreCase: true)
                : ProxyProtocol.Tcp;

            return await RunHandler.Handle(localPort, remoteHost!, remotePort, protocol);
        });

        return cmd;
    }
}
