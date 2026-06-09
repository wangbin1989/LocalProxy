using System.Text.Json;
using LocalProxy.Config;
using LocalProxy.Core;
using LocalProxy.Ipc;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
var logger = loggerFactory.CreateLogger<ConfigManager>();
var configManager = new ConfigManager(logger);
var ipcClient = new IpcClient(IpcProtocol.PipeName);

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

return args[0] switch
{
    "config" => await HandleConfig(args[1..]),
    "start" => await HandleStart(args[1..]),
    "stop" => await HandleStop(args[1..]),
    "status" => await HandleStatus(args[1..]),
    "--version" => HandleVersion(),
    "--help" => HandleHelp(),
    "-h" => HandleHelp(),
    _ => PrintUnknown(args[0])
};

static int HandleVersion()
{
    Console.WriteLine("localproxy 0.1.0");
    return 0;
}

static int HandleHelp()
{
    PrintUsage();
    return 0;
}

async Task<int> HandleConfig(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy config <add|remove|list> [options]");
        return 1;
    }

    return cmdArgs[0] switch
    {
        "add" => ConfigAdd(cmdArgs[1..]),
        "remove" => ConfigRemove(cmdArgs[1..]),
        "list" => ConfigList(cmdArgs[1..]),
        _ => PrintUnknown($"config {cmdArgs[0]}")
    };
}

int ConfigAdd(string[] cmdArgs)
{
    var name = GetArg(cmdArgs, "--name", "-n");
    var proto = GetArg(cmdArgs, "--proto", "-p") ?? "tcp";
    var localPort = GetArgInt(cmdArgs, "--local-port", "-l");
    var remoteHost = GetArg(cmdArgs, "--remote-host", "-r");
    var remotePort = GetArgInt(cmdArgs, "--remote-port", "-d");

    if (string.IsNullOrEmpty(name) || localPort == null || string.IsNullOrEmpty(remoteHost) || remotePort == null)
    {
        Console.Error.WriteLine("Usage: localproxy config add --name <name> --proto <tcp|udp|http> --local-port <port> --remote-host <host> --remote-port <port>");
        return 1;
    }

    var config = new ProxyConfig
    {
        Name = name,
        Protocol = Enum.TryParse<ProxyProtocol>(proto, ignoreCase: true, out var p) ? p : ProxyProtocol.Tcp,
        LocalPort = localPort.Value,
        RemoteHost = remoteHost,
        RemotePort = remotePort.Value,
    };

    try
    {
        var existing = configManager.Load();
        var updated = configManager.Add(existing, config);
        configManager.Save(updated);
        Console.WriteLine($"Proxy '{config.Name}' added.");
        return 0;
    }
    catch (ConfigException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 2;
    }
}

int ConfigRemove(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy config remove <name>");
        return 1;
    }

    var name = cmdArgs[0];
    try
    {
        var existing = configManager.Load();
        var updated = configManager.Remove(existing, name);
        configManager.Save(updated);
        Console.WriteLine($"Proxy '{name}' removed.");
        return 0;
    }
    catch (ConfigException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

int ConfigList(string[] cmdArgs)
{
    var asJson = cmdArgs.Contains("--json") || cmdArgs.Contains("-j");
    var configs = configManager.Load();

    if (asJson)
    {
        Console.WriteLine(JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true }));
    }
    else
    {
        if (configs.Count == 0)
        {
            Console.WriteLine("No proxy configurations found.");
            return 0;
        }
        Console.WriteLine($"{"Name",-20} {"Proto",-6} {"Local",-8} {"Remote",-30} {"Enabled",-8}");
        Console.WriteLine(new string('-', 72));
        foreach (var c in configs)
        {
            Console.WriteLine($"{c.Name,-20} {c.Protocol.ToString().ToUpperInvariant(),-6} {c.LocalPort,-8} {c.RemoteHost + ":" + c.RemotePort,-30} {c.Enabled,-8}");
        }
    }
    return 0;
}

async Task<int> HandleStart(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy start <name>");
        return 1;
    }

    var name = cmdArgs[0];
    return await SendIpcCommand(IpcProtocol.MethodStartTunnel, new { name }, $"Tunnel '{name}' started.");
}

async Task<int> HandleStop(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy stop <name>");
        return 1;
    }

    var name = cmdArgs[0];
    return await SendIpcCommand(IpcProtocol.MethodStopTunnel, new { name }, $"Tunnel '{name}' stopped.");
}

async Task<int> HandleStatus(string[] cmdArgs)
{
    var asJson = cmdArgs.Contains("--json") || cmdArgs.Contains("-j");
    var response = await ipcClient.SendAsync(new JsonRpcRequest { Method = IpcProtocol.MethodListTunnels });

    if (response.Error != null && response.Error.Code == IpcProtocol.ErrorServiceNotAvailable)
    {
        Console.WriteLine("Service not running.");
        return 3;
    }

    if (response.Error != null)
    {
        Console.Error.WriteLine($"Error: {response.Error.Message}");
        return 1;
    }

    if (asJson && response.Result.HasValue)
    {
        Console.WriteLine(JsonSerializer.Serialize(response.Result.Value, new JsonSerializerOptions { WriteIndented = true }));
    }
    else if (response.Result.HasValue)
    {
        var tunnels = response.Result.Value.EnumerateArray().ToArray();
        if (tunnels.Length == 0)
        {
            Console.WriteLine("No tunnels running.");
            return 0;
        }
        Console.WriteLine($"{"Name",-20} {"Status",-10} {"BytesIn",-12} {"BytesOut",-12} {"Conns",-6} {"Uptime",-12}");
        Console.WriteLine(new string('-', 72));
        foreach (var t in tunnels)
        {
            var tName = t.GetProperty("name").GetString() ?? "";
            var status = t.GetProperty("status").GetString() ?? "";
            var bytesIn = t.GetProperty("bytesIn").GetInt64();
            var bytesOut = t.GetProperty("bytesOut").GetInt64();
            var conns = t.GetProperty("activeConnections").GetInt32();
            var uptime = t.GetProperty("uptime").GetString() ?? "";
            Console.WriteLine($"{tName,-20} {status,-10} {FormatBytes(bytesIn),-12} {FormatBytes(bytesOut),-12} {conns,-6} {uptime,-12}");
        }
    }
    return 0;
}

async Task<int> SendIpcCommand(string method, object @params, string successMsg)
{
    var response = await ipcClient.SendAsync(new JsonRpcRequest
    {
        Method = method,
        Params = JsonSerializer.SerializeToElement(@params)
    });

    if (response.Error != null)
    {
        Console.Error.WriteLine($"Error: {response.Error.Message}");
        return response.Error.Code == IpcProtocol.ErrorServiceNotAvailable ? 3 : 1;
    }

    Console.WriteLine(successMsg);
    return 0;
}

static string FormatBytes(long bytes) => bytes switch
{
    >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
    >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
    >= 1024 => $"{bytes / 1024.0:F1} KB",
    _ => $"{bytes} B"
};

static string? GetArg(string[] args, string longName, string shortName)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == longName || args[i] == shortName)
        {
            if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                return args[i + 1];
        }
    }
    return null;
}

static int? GetArgInt(string[] args, string longName, string shortName)
{
    var val = GetArg(args, longName, shortName);
    return int.TryParse(val, out var n) ? n : null;
}

static int PrintUnknown(string cmd)
{
    Console.Error.WriteLine($"Unknown command: {cmd}");
    Console.Error.WriteLine("Run 'localproxy --help' for usage.");
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine(@"LocalProxy — 本地端口转发代理工具

Usage:
  localproxy config add   --name <name> --local-port <port> --remote-host <host> --remote-port <port> [--proto <tcp|udp|http>]
  localproxy config remove <name>
  localproxy config list  [--json]
  localproxy start <name>
  localproxy stop <name>
  localproxy status       [--json]
  localproxy --version
  localproxy --help");
}
