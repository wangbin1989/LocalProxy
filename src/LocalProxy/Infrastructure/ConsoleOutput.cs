using Spectre.Console;

namespace LocalProxy.Infrastructure;

public static class ConsoleOutput
{
    public static void ShowStartupPanel(int localPort, string remoteHost, int remotePort, string protocol)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .HideHeaders()
            .AddColumn("Key")
            .AddColumn("Value");

        table.Title = new TableTitle("代理已启动");

        table.AddRow("[bold]本地地址[/]", $":{localPort}");
        table.AddRow("[bold]远程地址[/]", $"{remoteHost}:{remotePort}");
        table.AddRow("[bold]协议类型[/]", protocol.ToUpperInvariant());

        AnsiConsole.Write(table);
    }

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

    public static void ConnectionInfo(string message)
    {
        AnsiConsole.MarkupLine($"[green]连接:[/] {message}");
    }
}
