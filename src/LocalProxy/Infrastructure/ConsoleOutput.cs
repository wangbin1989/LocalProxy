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

    public static void Errors(List<string> messages)
    {
        foreach (var msg in messages)
            AnsiConsole.MarkupLine($"[red]错误:[/] {msg}");
    }

    public static void Info(string message)
    {
        AnsiConsole.MarkupLine($"[blue]信息:[/] {message}");
    }

    public static void ConnectionInfo(string message)
    {
        AnsiConsole.MarkupLine($"[green]连接:[/] {message}");
    }
}
