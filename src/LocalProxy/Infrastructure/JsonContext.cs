using System.Text.Json.Serialization;
using LocalProxy.Services;

namespace LocalProxy.Infrastructure;

[JsonSerializable(typeof(ProxyConfig))]
[JsonSerializable(typeof(List<ProxyConfig>))]
internal partial class ProxyConfigJsonContext : JsonSerializerContext
{
}
