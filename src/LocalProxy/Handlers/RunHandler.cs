using LocalProxy.Infrastructure;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Handlers;

public static class RunHandler
{
    public static async Task<int> Handle(int localPort, string remoteHost, int remotePort, ProxyProtocol protocol)
    {
        var errors = new List<string>();

        if (localPort is < 0 or > 65535)
            errors.Add($"无效的本地端口: {localPort}，取值范围 0-65535");

        if (remotePort is < 0 or > 65535)
            errors.Add($"无效的远程端口: {remotePort}，取值范围 0-65535");

        if (string.IsNullOrWhiteSpace(remoteHost))
            errors.Add("远程主机地址不能为空");

        if (errors.Count > 0)
        {
            ConsoleOutput.Errors(errors);
            return 1;
        }

        ConsoleOutput.ShowStartupPanel(localPort, remoteHost, remotePort, protocol.ToString());

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            switch (protocol)
            {
                case ProxyProtocol.Tcp:
                case ProxyProtocol.Http:
                    await ProxyService.StartTcpProxyAsync(localPort, remoteHost, remotePort, cts.Token);
                    break;
                case ProxyProtocol.Udp:
                    await ProxyService.StartUdpProxyAsync(localPort, remoteHost, remotePort, cts.Token);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }

        ConsoleOutput.Info("代理已停止");
        return 0;
    }
}
