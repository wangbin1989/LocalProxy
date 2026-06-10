using System.Text.Json;
using LocalProxy.App.Models;
using LocalProxy.Ipc;

namespace LocalProxy.App.Services;

public class IpcService
{
    private readonly IpcClient _client = new(IpcProtocol.PipeName);

    public async Task<List<TunnelDisplayInfo>> GetTunnelsAsync()
    {
        var response = await _client.SendAsync(new JsonRpcRequest
        {
            Method = IpcProtocol.MethodListTunnels
        });

        if (response.Error != null || !response.Result.HasValue)
            return [];

        var tunnels = new List<TunnelDisplayInfo>();
        foreach (var t in response.Result.Value.EnumerateArray())
        {
            tunnels.Add(new TunnelDisplayInfo
            {
                Name = t.GetProperty("name").GetString() ?? "",
                Status = t.GetProperty("status").GetString() ?? "unknown",
                BytesIn = FormatBytes(t.GetProperty("bytesIn").GetInt64()),
                BytesOut = FormatBytes(t.GetProperty("bytesOut").GetInt64()),
                ActiveConnections = t.GetProperty("activeConnections").GetInt32(),
                Uptime = t.GetProperty("uptime").GetString() ?? "",
                IsRunning = (t.GetProperty("status").GetString() ?? "") == "running"
            });
        }
        return tunnels;
    }

    public async Task<bool> StartTunnelAsync(string name)
    {
        var response = await _client.SendAsync(new JsonRpcRequest
        {
            Method = IpcProtocol.MethodStartTunnel,
            Params = JsonSerializer.SerializeToElement(new { name })
        });
        return response.Error == null;
    }

    public async Task<bool> StopTunnelAsync(string name)
    {
        var response = await _client.SendAsync(new JsonRpcRequest
        {
            Method = IpcProtocol.MethodStopTunnel,
            Params = JsonSerializer.SerializeToElement(new { name })
        });
        return response.Error == null;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };
}
