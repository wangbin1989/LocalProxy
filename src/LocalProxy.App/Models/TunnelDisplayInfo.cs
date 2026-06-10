using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalProxy.App.Models;

public partial class TunnelDisplayInfo : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _protocol = string.Empty;

    [ObservableProperty]
    private int _localPort;

    [ObservableProperty]
    private string _remote = string.Empty;

    [ObservableProperty]
    private string _status = "Stopped";

    [ObservableProperty]
    private string _bytesIn = "0 B";

    [ObservableProperty]
    private string _bytesOut = "0 B";

    [ObservableProperty]
    private int _activeConnections;

    [ObservableProperty]
    private string _uptime = string.Empty;

    [ObservableProperty]
    private bool _isRunning;
}
