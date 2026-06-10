using LocalProxy.App.Services;
using LocalProxy.App.ViewModels;
using LocalProxy.App.Views;
using Microsoft.Extensions.Logging;

namespace LocalProxy.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<IpcService>();
        builder.Services.AddSingleton<TrayService>();
        builder.Services.AddSingleton<AutoStartService>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
