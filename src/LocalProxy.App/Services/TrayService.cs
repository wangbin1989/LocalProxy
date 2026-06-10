namespace LocalProxy.App.Services;

public class TrayService
{
    private bool _isTrayEnabled;

    public bool IsTrayEnabled
    {
        get => _isTrayEnabled;
        set
        {
            _isTrayEnabled = value;
            OnTrayEnabledChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<bool>? OnTrayEnabledChanged;

    public void ShowNotification(string title, string message)
    {
        // Platform-specific notification via tray
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // .NET MAUI doesn't have built-in notifications in all platforms.
            // On macOS, we could use NSUserNotification.
            // For now, log to console.
            System.Diagnostics.Debug.WriteLine($"[Tray] {title}: {message}");
        });
    }
}
