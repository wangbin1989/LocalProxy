namespace LocalProxy.Ipc;

public static class IpcProtocol
{
    public const string PipeName = "LocalProxy";

    // Method names
    public const string MethodListTunnels = "list_tunnels";
    public const string MethodStartTunnel = "start_tunnel";
    public const string MethodStopTunnel = "stop_tunnel";
    public const string MethodGetStats = "get_stats";
    public const string MethodReloadConfig = "reload_config";
    public const string MethodStopService = "stop_service";

    // Error codes
    public const int ErrorTunnelNotFound = -32001;
    public const int ErrorTunnelAlreadyRunning = -32002;
    public const int ErrorTunnelNotRunning = -32003;
    public const int ErrorServiceNotAvailable = -32004;
}
