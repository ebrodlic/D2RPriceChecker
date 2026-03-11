using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using D2RPriceChecker.Services;

namespace D2RPriceChecker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ScreenshotService? screenshots;
    private HotkeyManager? hotkeys;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;

        hotkeys = new HotkeyManager(handle);
        screenshots = new ScreenshotService();
  
        hotkeys.Register(Key.D, ModifierKeys.Control, async () =>
        {
            var tooltipImage = screenshots.CaptureTooltipRegion();
            //var text = await ocrService.ReadAsync(tooltipImage);
            //Dispatcher.Invoke(() => webView.ShowWithData(text));
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        hotkeys?.Dispose();
        base.OnClosed(e);
    }
}