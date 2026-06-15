using LocalProxy.Models;

namespace LocalProxy.Services;

public class ProxyConfig
{
    public string Name { get; set; } = "";
    public int LocalPort { get; set; }
    public string RemoteHost { get; set; } = "";
    public int RemotePort { get; set; }
    public ProxyProtocol Protocol { get; set; }
}
