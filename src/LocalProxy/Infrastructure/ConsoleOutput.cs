using Spectre.Console;

namespace LocalProxy.Infrastructure;

/// <summary>控制台输出辅助类，基于 Spectre.Console</summary>
public static class ConsoleOutput
{
    /// <summary>输出错误消息（红色）</summary>
    public static void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]错误:[/] {message}");
    }

    /// <summary>输出多条错误消息</summary>
    public static void Errors(List<string> messages)
    {
        foreach (var msg in messages)
            AnsiConsole.MarkupLine($"[red]错误:[/] {msg}");
    }

    /// <summary>输出信息消息（蓝色）</summary>
    public static void Info(string message)
    {
        AnsiConsole.MarkupLine($"[blue]信息:[/] {message}");
    }

    /// <summary>展示多代理启动面板，显示所有已启动代理的概览表格</summary>
    public static void ShowMultiStartupPanel(List<Services.ProxyConfig> configs)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("名称")
            .AddColumn("协议")
            .AddColumn("本地地址")
            .AddColumn("远程地址");

        table.Title = new TableTitle($"代理已启动 ({configs.Count} 个)");

        foreach (var c in configs)
        {
            table.AddRow(
                c.Name,
                c.Protocol.ToString().ToUpperInvariant(),
                $":{c.LocalPort}",
                $"{c.RemoteHost}:{c.RemotePort}");
        }

        AnsiConsole.Write(table);
    }

    /// <summary>渲染代理配置列表表格</summary>
    public static void RenderConfigTable(List<Services.ProxyConfig> configs)
    {
        if (configs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]暂无代理配置[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("名称")
            .AddColumn("协议")
            .AddColumn("本地地址")
            .AddColumn("远程地址")
            .AddColumn("状态")
            .AddColumn("描述");

        foreach (var c in configs)
        {
            var status = c.Enabled
                ? "[green]已启用[/]"
                : "[grey]已停用[/]";

            table.AddRow(
                c.Name,
                c.Protocol.ToString().ToUpperInvariant(),
                $":{c.LocalPort}",
                $"{c.RemoteHost}:{c.RemotePort}",
                status,
                "-");
        }

        AnsiConsole.Write(table);
    }

    /// <summary>输出成功消息（绿色勾）</summary>
    public static void Success(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }

    /// <summary>输出连接信息（绿色）</summary>
    public static void ConnectionInfo(string message)
    {
        AnsiConsole.MarkupLine($"[green]连接:[/] {message}");
    }

    /// <summary>弹出确认提示，返回用户选择</summary>
    public static bool Confirm(string message)
    {
        return AnsiConsole.Confirm($"[yellow]?[/] {message}");
    }
}
