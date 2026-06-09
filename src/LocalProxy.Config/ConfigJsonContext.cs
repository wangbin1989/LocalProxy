using System.Text.Json.Serialization;
using LocalProxy.Core;

namespace LocalProxy.Config;

[JsonSerializable(typeof(List<ProxyConfig>))]
[JsonSerializable(typeof(ProxyConfig))]
internal sealed partial class ConfigJsonContext : JsonSerializerContext
{
}
