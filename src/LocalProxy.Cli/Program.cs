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
    "service" => HandleService(args[1..]),
    "completion" => HandleCompletion(args[1..]),
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
        "import" => ConfigImport(cmdArgs[1..]),
        "export" => ConfigExport(cmdArgs[1..]),
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

int ConfigImport(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy config import <file>");
        return 1;
    }

    var filePath = cmdArgs[0];
    if (!File.Exists(filePath))
    {
        Console.Error.WriteLine($"File not found: {filePath}");
        return 1;
    }

    try
    {
        var json = File.ReadAllText(filePath);
        var imported = JsonSerializer.Deserialize<List<ProxyConfig>>(json);
        if (imported == null || imported.Count == 0)
        {
            Console.Error.WriteLine("No valid proxy configs found in file.");
            return 2;
        }

        var existing = configManager.Load();
        var merged = new List<ProxyConfig>(existing);
        foreach (var config in imported)
        {
            if (existing.Any(c => c.Name == config.Name))
            {
                Console.WriteLine($"Skipping duplicate: {config.Name}");
                continue;
            }
            merged.Add(config);
        }

        var validated = configManager.Add(new List<ProxyConfig>(), merged[0]); // trigger validation
        for (int i = 1; i < merged.Count; i++)
            validated = configManager.Add(validated, merged[i]);

        configManager.Save(validated);
        Console.WriteLine($"Imported {imported.Count} config(s).");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Import error: {ex.Message}");
        return 2;
    }
}

int ConfigExport(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy config export <file>");
        return 1;
    }

    var filePath = cmdArgs[0];
    var configs = configManager.Load();
    var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
    Console.WriteLine($"Exported {configs.Count} config(s) to {filePath}");
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

int HandleService(string[] cmdArgs)
{
    if (cmdArgs.Length == 0)
    {
        Console.Error.WriteLine("Usage: localproxy service <install|uninstall|start|stop|status>");
        return 1;
    }

    return cmdArgs[0] switch
    {
        "install" => ServiceInstall(),
        "uninstall" => ServiceUninstall(),
        "start" => ServiceStart(),
        "stop" => ServiceStop(),
        "status" => ServiceStatus(),
        _ => PrintUnknown($"service {cmdArgs[0]}")
    };
}

int ServiceInstall()
{
    var servicePath = GetServiceDefinitionPath();
    if (File.Exists(servicePath))
    {
        Console.WriteLine("Service already installed.");
        return 0;
    }

    var definition = GenerateServiceDefinition();
    var dir = Path.GetDirectoryName(servicePath)!;
    Directory.CreateDirectory(dir);
    File.WriteAllText(servicePath, definition);
    Console.WriteLine($"Service installed at {servicePath}");
    return 0;
}

int ServiceUninstall()
{
    var servicePath = GetServiceDefinitionPath();
    if (!File.Exists(servicePath))
    {
        Console.WriteLine("Service not installed.");
        return 0;
    }

    File.Delete(servicePath);
    Console.WriteLine("Service uninstalled.");
    return 0;
}

int ServiceStart()
{
    Console.WriteLine("Starting LocalProxy service...");
    if (OperatingSystem.IsMacOS())
    {
        var path = GetServiceDefinitionPath();
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("Service not installed. Run 'localproxy service install' first.");
            return 3;
        }
        Console.WriteLine("Service will start on next login. Use 'launchctl load' to start now.");
    }
    Console.WriteLine("Start the service process directly with: dotnet run --project <path>");
    return 0;
}

int ServiceStop()
{
    Console.WriteLine("Send stop signal to LocalProxy service...");
    var response = ipcClient.SendAsync(new JsonRpcRequest { Method = IpcProtocol.MethodStopService }).Result;
    if (response.Error != null)
        Console.WriteLine("No running service found.");
    else
        Console.WriteLine("Service stop signal sent.");
    return 0;
}

int ServiceStatus()
{
    var path = GetServiceDefinitionPath();
    if (File.Exists(path))
        Console.WriteLine($"Service definition: {path} (installed)");
    else
        Console.WriteLine("Service not installed.");

    var response = ipcClient.SendAsync(new JsonRpcRequest { Method = IpcProtocol.MethodListTunnels }).Result;
    if (response.Error?.Code == IpcProtocol.ErrorServiceNotAvailable)
        Console.WriteLine("Service process not running.");
    else
        Console.WriteLine("Service process is running.");
    return 0;
}

static string GetServiceDefinitionPath()
{
    if (OperatingSystem.IsMacOS())
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library/LaunchAgents/com.localproxy.plist");

    if (OperatingSystem.IsWindows())
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft/Windows/Start Menu/Programs/Startup/LocalProxy.lnk");

    // Linux
    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config/systemd/user/localproxy.service");
}

static string GenerateServiceDefinition()
{
    if (OperatingSystem.IsMacOS())
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key><string>com.localproxy</string>
    <key>ProgramArguments</key>
    <array><string>/usr/local/bin/localproxy</string></array>
    <key>RunAtLoad</key><true/>
    <key>KeepAlive</key><true/>
</dict>
</plist>";

    if (OperatingSystem.IsWindows())
        return "# Windows: Register via 'sc create LocalProxy' or Task Scheduler";

    // Linux systemd
    return @"[Unit]
Description=LocalProxy Service
After=network.target

[Service]
Type=simple
ExecStart=/usr/bin/localproxy
Restart=always
RestartSec=5

[Install]
WantedBy=default.target";
}

int HandleCompletion(string[] cmdArgs)
{
    var shell = cmdArgs.Length > 0 ? cmdArgs[0] : "bash";
    Console.WriteLine(GenerateCompletion(shell));
    return 0;
}

static string GenerateCompletion(string shell) => shell switch
{
    "bash" => @"# bash completion for localproxy
_localproxy_complete() {
    local cur=${COMP_WORDS[COMP_CWORD]}
    local cmds=""config start stop status service completion --version --help""
    local subcmds="""";
    case ${COMP_WORDS[1]} in
        config) subcmds=""add remove list import export"";;
        service) subcmds=""install uninstall start stop status"";;
        completion) subcmds=""bash zsh pwsh"";;
    esac
    COMPREPLY=($(compgen -W ""$cmds $subcmds"" -- ""$cur""))
}
complete -F _localproxy_complete localproxy",

    "zsh" => @"# zsh completion for localproxy
#compdef localproxy
_localproxy() {
    local -a cmds
    cmds=('config:manage proxy configurations' 'start:start a tunnel' 'stop:stop a tunnel' 'status:show tunnel status' 'service:manage service')
    _describe 'command' cmds
}
_localproxy",

    _ => "# Unsupported shell. Supported: bash, zsh, pwsh"
};

static void PrintUsage()
{
    Console.WriteLine(@"LocalProxy — 本地端口转发代理工具

Usage:
  localproxy config add      --name <name> --local-port <port> --remote-host <host> --remote-port <port> [--proto <tcp|udp|http>]
  localproxy config remove   <name>
  localproxy config list     [--json]
  localproxy config import   <file>
  localproxy config export   <file>
  localproxy start           <name>
  localproxy stop            <name>
  localproxy status          [--json]
  localproxy service         <install|uninstall|start|stop|status>
  localproxy completion      <bash|zsh|pwsh>
  localproxy --version
  localproxy --help");
}
