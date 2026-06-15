using System.Text.Json;
using LocalProxy.Infrastructure;

namespace LocalProxy.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        TypeInfoResolver = ProxyConfigJsonContext.Default,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static async Task<List<ProxyConfig>> LoadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ProxyConfig>>(stream, s_options)
               ?? throw new InvalidOperationException("无法解析配置文件");
    }
}
