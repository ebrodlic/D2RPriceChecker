using D2RPriceChecker.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace D2RPriceChecker;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public CacheService Cache { get; private set; } = null!;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Create root and cache dirs
        Cache = new CacheService();

        // 2. Initialize logging into Logs/
        LoggingService.Initialize(Cache.RootDir);
      
        var main = new MainWindow();
        MainWindow = main;

        main.Show();

        LoggingService.Info("Application started");
    }
}

