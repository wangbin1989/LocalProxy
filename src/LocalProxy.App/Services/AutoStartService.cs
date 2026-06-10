namespace LocalProxy.App.Services;

public class AutoStartService
{
    private bool _isAutoStartEnabled;

    public bool IsAutoStartEnabled
    {
        get => _isAutoStartEnabled;
        set
        {
            _isAutoStartEnabled = value;
            ConfigureAutoStart(value);
        }
    }

    private static void ConfigureAutoStart(bool enable)
    {
        if (OperatingSystem.IsMacOS())
        {
            var plistPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/LaunchAgents/com.localproxy.plist");

            if (enable)
            {
                var dir = Path.GetDirectoryName(plistPath)!;
                Directory.CreateDirectory(dir);

                var plist = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key><string>com.localproxy</string>
    <key>ProgramArguments</key>
    <array><string>/usr/local/bin/localproxy</string></array>
    <key>RunAtLoad</key><true/>
    <key>KeepAlive</key><true/>
</dict>
</plist>";
                File.WriteAllText(plistPath, plist);
            }
            else
            {
                if (File.Exists(plistPath))
                    File.Delete(plistPath);
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            var startupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                "LocalProxy.lnk");

            if (enable)
                File.WriteAllText(startupPath, ""); // Placeholder
            else if (File.Exists(startupPath))
                File.Delete(startupPath);
        }
        // Linux: ~/.config/autostart/localproxy.desktop
    }
}
