using LocalProxy.Infrastructure;
using LocalProxy.Models;
using LocalProxy.Services;

namespace LocalProxy.Handlers;

/// <summary>Run 命令处理程序：校验并启动多个代理</summary>
public static class RunHandler
{
    /// <summary>校验配置并并行启动所有代理，Ctrl+C 停止</summary>
    public static async Task<int> HandleMultiple(List<ProxyConfig> configs)
    {
        var errors = new List<string>();
        foreach (var c in configs)
            errors.AddRange(Validate(c.LocalPort, c.RemoteHost, c.RemotePort));

        if (errors.Count > 0)
        {
            ConsoleOutput.Errors(errors);
            return 1;
        }

        ConsoleOutput.ShowMultiStartupPanel(configs);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var tasks = configs.Select(c =>
            RunProxyAsync(c.LocalPort, c.RemoteHost, c.RemotePort, c.Protocol, cts.Token));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }

        ConsoleOutput.Info("代理已停止");
        return 0;
    }

    /// <summary>校验端口范围和主机地址，返回错误消息列表</summary>
    private static List<string> Validate(int localPort, string remoteHost, int remotePort)
    {
        var errors = new List<string>();

        if (localPort is < 0 or > 65535)
            errors.Add($"无效的本地端口: {localPort}，取值范围 0-65535");

        if (remotePort is < 0 or > 65535)
            errors.Add($"无效的远程端口: {remotePort}，取值范围 0-65535");

        if (string.IsNullOrWhiteSpace(remoteHost))
            errors.Add("远程主机地址不能为空");

        return errors;
    }

    /// <summary>根据协议类型调用对应的代理启动方法</summary>
    private static async Task RunProxyAsync(
        int localPort, string remoteHost, int remotePort, ProxyProtocol protocol, CancellationToken ct)
    {
        try
        {
            switch (protocol)
            {
                case ProxyProtocol.Tcp:
                    await ProxyService.StartTcpProxyAsync(localPort, remoteHost, remotePort, ct);
                    break;
                case ProxyProtocol.Http:
                    await ProxyService.StartHttpProxyAsync(localPort, remoteHost, remotePort, ct);
                    break;
                case ProxyProtocol.Udp:
                    await ProxyService.StartUdpProxyAsync(localPort, remoteHost, remotePort, ct);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // 正常退出
        }
    }
}
