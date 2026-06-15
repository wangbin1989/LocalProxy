using Spectre.Console;

namespace LocalProxy.Infrastructure;

public static class ConsoleOutput
{
    public static void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]错误:[/] {message}");
    }

    public static void Errors(List<string> messages)
    {
        foreach (var msg in messages)
            AnsiConsole.MarkupLine($"[red]错误:[/] {msg}");
    }

    public static void Info(string message)
    {
        AnsiConsole.MarkupLine($"[blue]信息:[/] {message}");
    }

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

    public static void Success(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }

    public static void ConnectionInfo(string message)
    {
        AnsiConsole.MarkupLine($"[green]连接:[/] {message}");
    }

    public static bool Confirm(string message)
    {
        return AnsiConsole.Confirm($"[yellow]?[/] {message}");
    }
}
