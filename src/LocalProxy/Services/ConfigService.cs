using System.Text.Json;
using System.Text.Json.Serialization;
using LocalProxy.Infrastructure;

namespace LocalProxy.Services;

/// <summary>配置文件读写服务</summary>
public static class ConfigService
{
    /// <summary>默认配置文件路径：~/.localproxy.json</summary>
    public static string DefaultConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".localproxy.json");

    private static readonly JsonSerializerOptions s_options = new()
    {
        TypeInfoResolver = ProxyConfigJsonContext.Default,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    /// <summary>从文件加载代理配置列表，文件不存在时返回空列表</summary>
    public static async Task<List<ProxyConfig>> LoadAsync(string path)
    {
        if (!File.Exists(path))
            return [];

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ProxyConfig>>(stream, s_options)
               ?? [];
    }

    /// <summary>保存代理配置列表到文件，自动创建目录</summary>
    public static async Task SaveAsync(string path, List<ProxyConfig> configs)
    {
        var dir = Path.GetDirectoryName(path);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, configs, s_options);
    }

    /// <summary>添加代理配置，重名时抛出异常</summary>
    public static async Task AddAsync(string path, ProxyConfig config)
    {
        var configs = await LoadAsync(path);

        if (configs.Any(c => string.Equals(c.Name, config.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"代理 '{config.Name}' 已存在");

        configs.Add(config);
        await SaveAsync(path, configs);
    }

    /// <summary>更新指定名称的代理配置，不存在时抛出异常</summary>
    public static async Task UpdateAsync(string path, string name, ProxyConfig updated)
    {
        var configs = await LoadAsync(path);
        var index = configs.FindIndex(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            throw new InvalidOperationException($"代理 '{name}' 不存在");

        configs[index] = updated;
        await SaveAsync(path, configs);
    }

    /// <summary>删除指定名称的代理配置，不存在时抛出异常</summary>
    public static async Task RemoveAsync(string path, string name)
    {
        var configs = await LoadAsync(path);
        var removed = configs.RemoveAll(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
            throw new InvalidOperationException($"代理 '{name}' 不存在");

        await SaveAsync(path, configs);
    }

    /// <summary>设置代理的启用/停用状态</summary>
    public static async Task SetEnabledAsync(string path, string name, bool enabled)
    {
        var configs = await LoadAsync(path);
        var config = configs.Find(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (config is null)
            throw new InvalidOperationException($"代理 '{name}' 不存在");

        config.Enabled = enabled;
        await SaveAsync(path, configs);
    }
}
