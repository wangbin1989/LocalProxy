using System.Text.Json.Serialization;
using LocalProxy.Services;

namespace LocalProxy.Infrastructure;

/// <summary>JSON 源生成上下文，用于 AOT 兼容的序列化</summary>
[JsonSerializable(typeof(ProxyConfig))]
[JsonSerializable(typeof(List<ProxyConfig>))]
internal partial class ProxyConfigJsonContext : JsonSerializerContext
{
}
