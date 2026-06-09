using System.Text.Json;
using System.Text.Json.Serialization;
using LocalProxy.Core;
using Microsoft.Extensions.Logging;

namespace LocalProxy.Config;

public sealed class ConfigManager
{
    private readonly string _configPath;
    private readonly string _configDir;
    private readonly ILogger<ConfigManager> _logger;
    private FileSystemWatcher? _watcher;
    private DateTime _lastReload;

    public event EventHandler<IReadOnlyList<ProxyConfig>>? ConfigChanged;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        TypeInfoResolver = ConfigJsonContext.Default,
    };

    private static readonly JsonSerializerOptions JsonOptionsRead = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        TypeInfoResolver = ConfigJsonContext.Default,
    };

    public ConfigManager(ILogger<ConfigManager> logger)
    {
        _configDir = GetConfigDirectory();
        _configPath = Path.Combine(_configDir, "config.json");
        _logger = logger;
    }

    public IReadOnlyList<ProxyConfig> Load()
    {
        if (!File.Exists(_configPath))
        {
            _logger.LogInformation("No config file found at {Path}, using defaults", _configPath);
            return [];
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var configs = JsonSerializer.Deserialize<List<ProxyConfig>>(json, JsonOptions) ?? [];
            _logger.LogInformation("Loaded {Count} proxy configs from {Path}", configs.Count, _configPath);
            return Validate(configs);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse config file {Path}", _configPath);
            throw new ConfigException($"Invalid config file: {ex.Message}", ex);
        }
    }

    public void Save(IReadOnlyList<ProxyConfig> configs)
    {
        var validated = Validate(configs);
        var json = JsonSerializer.Serialize(validated, JsonOptions);

        Directory.CreateDirectory(_configDir);

        // Atomic write: write to temp file, then rename
        var tempPath = _configPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _configPath, overwrite: true);

        // Keep backups
        var backupDir = Path.Combine(_configDir, "backups");
        Directory.CreateDirectory(backupDir);
        var backupPath = Path.Combine(backupDir, $"config.{DateTime.UtcNow:yyyyMMddHHmmss}.json");
        File.WriteAllText(backupPath, json);

        // Retain only last 5 backups
        var backups = Directory.GetFiles(backupDir, "config.*.json").OrderDescending().ToArray();
        foreach (var old in backups.Skip(5))
            File.Delete(old);

        _logger.LogInformation("Saved {Count} proxy configs to {Path}", validated.Count, _configPath);
    }

    public void StartWatching()
    {
        if (_watcher != null) return;

        _watcher = new FileSystemWatcher(_configDir, "config.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnConfigFileChanged;
        _logger.LogInformation("Started watching config file at {Path}", _configPath);
    }

    public void StopWatching()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    public IReadOnlyList<ProxyConfig> Add(IReadOnlyList<ProxyConfig> existing, ProxyConfig config)
    {
        var list = new List<ProxyConfig>(existing);

        if (list.Any(c => c.Name == config.Name))
            throw new ConfigException($"A proxy with name '{config.Name}' already exists.");

        list.Add(config);
        return Validate(list);
    }

    public IReadOnlyList<ProxyConfig> Remove(IReadOnlyList<ProxyConfig> existing, string name)
    {
        var list = new List<ProxyConfig>(existing);
        var removed = list.RemoveAll(c => c.Name == name);

        if (removed == 0)
            throw new ConfigException($"No proxy found with name '{name}'.");

        return list;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: ignore events within 500ms
        if ((DateTime.UtcNow - _lastReload).TotalMilliseconds < 500) return;
        _lastReload = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Config file changed, reloading...");
            var configs = Load();
            ConfigChanged?.Invoke(this, configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload config on change");
        }
    }

    private static IReadOnlyList<ProxyConfig> Validate(IReadOnlyList<ProxyConfig> configs)
    {
        var names = new HashSet<string>();
        var ports = new HashSet<int>();

        foreach (var c in configs)
        {
            if (string.IsNullOrWhiteSpace(c.Name))
                throw new ConfigException("Proxy name is required.");

            if (!names.Add(c.Name))
                throw new ConfigException($"Duplicate proxy name: '{c.Name}'.");

            if (c.LocalPort is < 1 or > 65535)
                throw new ConfigException($"Proxy '{c.Name}': local port {c.LocalPort} is invalid. Must be 1-65535.");

            if (c.RemotePort is < 1 or > 65535)
                throw new ConfigException($"Proxy '{c.Name}': remote port {c.RemotePort} is invalid. Must be 1-65535.");

            if (!ports.Add(c.LocalPort))
                throw new ConfigException($"Proxy '{c.Name}': local port {c.LocalPort} conflicts with another proxy.");

            if (string.IsNullOrWhiteSpace(c.RemoteHost))
                throw new ConfigException($"Proxy '{c.Name}': remote host is required.");

            if (c.MaxConnections < 1)
                throw new ConfigException($"Proxy '{c.Name}': max connections must be at least 1.");

            if (c.TimeoutSeconds < 1)
                throw new ConfigException($"Proxy '{c.Name}': timeout must be at least 1 second.");
        }

        return configs;
    }

    private static string GetConfigDirectory()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalProxy");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".localproxy");
    }
}

public class ConfigException : Exception
{
    public ConfigException(string message) : base(message) { }
    public ConfigException(string message, Exception inner) : base(message, inner) { }
}
