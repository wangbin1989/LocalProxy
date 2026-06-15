using System.CommandLine;
using LocalProxy.Handlers;
using LocalProxy.Models;

namespace LocalProxy.Commands;

public static class RunCommand
{
    public static Command Build()
    {
        var cmd = new Command("run", "启动代理隧道");

        var localPortOption = new Option<int>("--local-port", "-l")
            { Description = "本地监听端口 (0-65535)" };
        var remoteHostOption = new Option<string>("--remote-host", "-H")
            { Description = "远程目标主机地址" };
        var remotePortOption = new Option<int>("--remote-port", "-p")
            { Description = "远程目标端口 (0-65535)" };
        var protocolOption = new Option<ProxyProtocol>("--protocol", "-P")
            { Description = "代理协议类型 (tcp, udp, http)" };

        cmd.Add(localPortOption);
        cmd.Add(remoteHostOption);
        cmd.Add(remotePortOption);
        cmd.Add(protocolOption);

        cmd.SetAction(async parseResult =>
        {
            var localPort = parseResult.GetValue(localPortOption);
            var remoteHost = parseResult.GetValue(remoteHostOption);
            var remotePort = parseResult.GetValue(remotePortOption);
            var protocol = parseResult.GetValue(protocolOption);

            return await RunHandler.Handle(localPort, remoteHost!, remotePort, protocol);
        });

        return cmd;
    }
}
