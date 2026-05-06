using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using D2RCompanion.Core.Items;
using D2RCompanion.Services;
using D2RCompanion.UI.AppCore;
using D2RCompanion.UI.Controls;
using D2RCompanion.UI.Services;
using D2RCompanion.UI.Traderie;
using D2RCompanion.UI.Util;
using D2RCompanion.UI.ViewModels;
using D2RCompanion.UI.Views;
using D2RCompanion.UI.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace D2RCompanion.UI;

public partial class App : System.Windows.Application
{
    private IConfiguration _config = null!;
    private IServiceProvider _provider = null!;
    private ILogger<App> _logger = null!;

    private AppInfo _appInfo = null!;
    private AppPaths _appPaths = null!;
    private AppEnvironment _appEnvironment = null!;

    private NotifyIcon _trayIcon = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetupConfig();
        SetupCore();
        SetupLogging();
        SetupDI();

        // TODO - mb initialize simple services here, like settings
        InitializeTray();
        InitializeView();

        _logger.LogInformation("Application Started");

        _ = Task.Run(InitializeBackgroundAsync);
    }

    private void SetupConfig()
    {
        _config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
    private void SetupCore()
    {
        _appInfo = new AppInfo();
        _appPaths = new AppPaths(_appInfo);
        _appEnvironment = new AppEnvironment();

        _appPaths.EnsureCreated();
    }
    private void SetupLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_config)
            .WriteTo.File(
                Path.Combine(_appPaths.Logs, "log-.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    private void SetupDI()
    {
        var services = new ServiceCollection();

        // Core
        services.AddSingleton(_config);
        services.AddSingleton(_appInfo);
        services.AddSingleton(_appPaths);

        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        services.AddSingleton<IItemBaseNameProvider, FileItemBaseNameProvider>();

        // Services
        services.AddSingleton<SettingsService>(); 
        services.AddSingleton<ScreenshotService>();
        services.AddSingleton<PipelineService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<UpdateService>();

        // TODO: temporarily to debug saved images:
        services.AddSingleton<CacheService>();

        //extra
        services.AddSingleton(new OcrService("Models/d2r_tooltip_crnn_best.onnx"));     

        //UI
        services.AddSingleton<OverlayWindow>();
        services.AddSingleton<OverlayViewModel>();

        services.AddSingleton<HomeView>();
        services.AddSingleton<HomeViewModel>();

        services.AddSingleton<SettingsView>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<PriceCheckView>();
        services.AddSingleton<PriceCheckViewModel>();

        services.AddSingleton<TraderieWebViewControl>();
        services.AddSingleton<TraderieClient>();
       
        _provider = services.BuildServiceProvider();

        // Needed for tray, consider moving to separate method
        _logger = _provider.GetRequiredService<ILogger<App>>();
    }

    private void InitializeTray()
    {
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "1.ico");
        var iconText = _appInfo.Name;

        var versionText = _appEnvironment.IsDevelopment ? _appInfo.Version : VelopackLocator.Current.CurrentlyInstalledVersion?.ToString();

        _trayIcon = new NotifyIcon()
        {
            Icon = new Icon(iconPath),
            Visible = true,
            Text = $"{_appInfo.Name} - {versionText}"
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (s, e) => { MainWindow.Show(); });
        menu.Items.Add("Check for Updates", null, async (s, e) => {
            await CheckForUpdatesAsync();
        }); 
        menu.Items.Add("Exit", null, (s, e) => { System.Windows.Application.Current.Shutdown(); });

        _trayIcon.ContextMenuStrip = menu;
    }

    private async Task CheckForUpdatesAsync()
    {
        _logger.LogDebug("Checking for updates");

        // If local development project/machine
        if (_appEnvironment.IsDevelopment)
            return;

        try
        {
            var updates = _provider.GetRequiredService<UpdateService>();
            var update = await updates.CheckAsync();

            if (update == null)
                return;

            _logger.LogInformation("Update found, downloading!");

            await updates.DownloadAsync(update);

            var result = System.Windows.MessageBox.Show(
                "Update Ready, the app will restart",
                "Update Ready",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);

            if(result == MessageBoxResult.OK)
            {
                updates.ApplyAndRestart(update);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in update check: " +  ex.Message);
        }
    }

    private void InitializeView()
    {
        var overlay = _provider.GetRequiredService<OverlayWindow>();

        overlay.Show();
        overlay.HideOverlayContent();

        MainWindow = overlay;
    }

    private async Task InitializeBackgroundAsync()
    {
        try
        {
            var settingsTask = Task.Run(() => _provider.GetRequiredService<SettingsService>().Initialize());
            var ocrTask = Task.Run(() => _provider.GetRequiredService<OcrService>().InitializeAsync());

            await Task.WhenAll(settingsTask, ocrTask);

            //var traderieWindow = _provider.GetRequiredService<TraderieWindow>();

            //await traderieWindow.Preload();
            //await traderieWindow.InitializeAsync();

            //await Task.Delay(300);

            //// Try to obtain session info for future use
            //await traderieWindow.TryLoadSessionAsync();

            //while (!traderieWindow.IsLoggedIn)
            //{
            //    traderieWindow.Show();
            //    await Task.Delay(5000);
            //}

            // If no session info, show the traderie window so user can log in for session data
            //if (!traderieWindow.IsLoggedIn)
            //    traderieWindow.Show();

            //WeakReferenceMessenger.Default.Send(new AppReadyMessage());
        }
        catch (Exception ex)
        {

        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = _provider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application Exit");

        Log.CloseAndFlush();

        //if (MainWindow is MainWindow main)
        //    main.Cleanup();

        base.OnExit(e);
    }
}

