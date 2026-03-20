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
    public DatasetManager DatasetManager { get; private set; } = new DatasetManager();
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }
}

