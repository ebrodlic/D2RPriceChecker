using D2RPriceChecker.Services;
using D2RPriceChecker.UI.Views;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows;

namespace D2RPriceChecker.UI;

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

        // 1. Create root and cache dirs into Cache/
        Cache = new CacheService();

        // 2. Initialize logging into Logs/
        LoggingService.Initialize(Cache.RootDir);

        // 3. Load settings
        Settings = new SettingsService(Cache.RootDir);
        Settings.Initialize();

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

