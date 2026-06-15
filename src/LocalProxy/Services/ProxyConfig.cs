using LocalProxy.Models;

namespace LocalProxy.Services;

/// <summary>单个代理配置</summary>
public class ProxyConfig
{
    /// <summary>代理名称</summary>
    public string Name { get; set; } = "";
    /// <summary>本地监听端口</summary>
    public int LocalPort { get; set; }
    /// <summary>远程目标主机</summary>
    public string RemoteHost { get; set; } = "";
    /// <summary>远程目标端口</summary>
    public int RemotePort { get; set; }
    /// <summary>代理协议</summary>
    public ProxyProtocol Protocol { get; set; }
    /// <summary>是否启用</summary>
    public bool Enabled { get; set; } = true;
}
