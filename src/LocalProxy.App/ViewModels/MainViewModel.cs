using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalProxy.App.Models;
using LocalProxy.App.Services;

namespace LocalProxy.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IpcService _ipcService;

    [ObservableProperty]
    private ObservableCollection<TunnelDisplayInfo> _tunnels = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public MainViewModel(IpcService ipcService)
    {
        _ipcService = ipcService;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = "Refreshing...";

        try
        {
            var tunnels = await _ipcService.GetTunnelsAsync();
            Tunnels = new ObservableCollection<TunnelDisplayInfo>(tunnels);
            StatusMessage = $"{tunnels.Count} tunnel(s)";
        }
        catch
        {
            StatusMessage = "Service not available";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartTunnelAsync(TunnelDisplayInfo tunnel)
    {
        if (tunnel == null) return;
        StatusMessage = $"Starting {tunnel.Name}...";
        var ok = await _ipcService.StartTunnelAsync(tunnel.Name);
        StatusMessage = ok ? $"{tunnel.Name} started" : $"Failed to start {tunnel.Name}";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task StopTunnelAsync(TunnelDisplayInfo tunnel)
    {
        if (tunnel == null) return;
        StatusMessage = $"Stopping {tunnel.Name}...";
        var ok = await _ipcService.StopTunnelAsync(tunnel.Name);
        StatusMessage = ok ? $"{tunnel.Name} stopped" : $"Failed to stop {tunnel.Name}";
        await RefreshAsync();
    }
}
