using D2RPriceChecker.Services;
using D2RPriceChecker.Windows;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows;

namespace D2RPriceChecker;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public CacheService Cache { get; private set; } = null!;
    public SettingsService Settings { get; private set; } = null!;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Create root and cache dirs
        Cache = new CacheService();

        // 2. Load settings
        Settings = new SettingsService(Cache.RootDir);

        // 3. Initialize logging into Logs/
        LoggingService.Initialize(Cache.RootDir);

        // 4. Show main window (splash)
        MainWindow = new SplashWindow();
        MainWindow.Show();

        LoggingService.Info("Application started");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if(MainWindow is SplashWindow)
            ((SplashWindow)MainWindow).Cleanup();

        base.OnExit(e);
        //LoggingService.Info("Application exited");
    }
}

