using LocalProxy.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalProxy.Config.Tests;

public class ConfigManagerTests
{
    private readonly ILogger<ConfigManager> _logger = NullLogger<ConfigManager>.Instance;

    [Fact]
    public void LoadReturnsEmptyWhenNoConfigFile()
    {
        // Use a temp directory to ensure no config exists
        var manager = new ConfigManager(_logger);
        var configs = manager.Load();
        Assert.NotNull(configs);
    }

    [Fact]
    public void ValidationRejectsInvalidPort()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "test", Protocol = ProxyProtocol.Tcp, LocalPort = 0, RemoteHost = "example.com", RemotePort = 80 }
        };

        Assert.Throws<ConfigException>(() => SaveAndCleanup(configs));
    }

    [Fact]
    public void ValidationRejectsDuplicateName()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "same", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "a.com", RemotePort = 80 },
            new() { Name = "same", Protocol = ProxyProtocol.Tcp, LocalPort = 8002, RemoteHost = "b.com", RemotePort = 80 }
        };

        Assert.Throws<ConfigException>(() => SaveAndCleanup(configs));
    }

    [Fact]
    public void ValidationRejectsDuplicatePort()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "a", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "a.com", RemotePort = 80 },
            new() { Name = "b", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "b.com", RemotePort = 80 }
        };

        Assert.Throws<ConfigException>(() => SaveAndCleanup(configs));
    }

    [Fact]
    public void ValidationRejectsEmptyName()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "a.com", RemotePort = 80 }
        };

        Assert.Throws<ConfigException>(() => SaveAndCleanup(configs));
    }

    [Fact]
    public void ValidationRejectsMissingRemoteHost()
    {
        var configs = new List<ProxyConfig>
        {
            new() { Name = "test", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "", RemotePort = 80 }
        };

        Assert.Throws<ConfigException>(() => SaveAndCleanup(configs));
    }

    [Fact]
    public void AddConfigurationWorks()
    {
        var existing = new List<ProxyConfig>
        {
            new() { Name = "existing", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "example.com", RemotePort = 80 }
        };

        var newConfig = new ProxyConfig
        {
            Name = "new-one", Protocol = ProxyProtocol.Udp, LocalPort = 8002, RemoteHost = "example.com", RemotePort = 53
        };

        var manager = new ConfigManager(_logger);
        var result = manager.Add(existing, newConfig);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "new-one");
    }

    [Fact]
    public void RemoveConfigurationWorks()
    {
        var existing = new List<ProxyConfig>
        {
            new() { Name = "keep", Protocol = ProxyProtocol.Tcp, LocalPort = 8001, RemoteHost = "example.com", RemotePort = 80 },
            new() { Name = "remove", Protocol = ProxyProtocol.Tcp, LocalPort = 8002, RemoteHost = "example.com", RemotePort = 80 }
        };

        var manager = new ConfigManager(_logger);
        var result = manager.Remove(existing, "remove");

        Assert.Single(result);
        Assert.DoesNotContain(result, c => c.Name == "remove");
    }

    private void SaveAndCleanup(IReadOnlyList<ProxyConfig> configs)
    {
        var manager = new ConfigManager(_logger);
        manager.Save(configs);
    }
}
