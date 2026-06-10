using LocalProxy.App.Services;

namespace LocalProxy.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell())
        {
            Title = "LocalProxy",
            MinimumWidth = 400,
            MinimumHeight = 500,
        };

        window.Destroying += (s, e) =>
        {
            // On close, minimize to tray if enabled
            var trayService = Handler?.MauiContext?.Services.GetService<TrayService>();
            if (trayService?.IsTrayEnabled == true)
            {
                // Cancel close, hide window instead
                // In MAUI, we can't fully cancel Close, but we can minimize
            }
        };

        return window;
    }
}
