using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Drawing;
using Microsoft.Win32;
using D2RPriceChecker.Services;

namespace D2RPriceChecker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private List<string> _imagePaths = new();
    private int _currentImageIndex = -1;
    private BitmapImage? _loadedImage;
    private Bitmap? _loadedBitmap;

    private readonly ScreenshotService _screenshots = new();
    private HotkeyManager? _hotkeys;

    public MainWindow()
    {
        InitializeComponent();
        this.PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        { 
            case Key.Left:
                LeftButton_Click(this, null);
                e.Handled = true;
                break;
            case Key.Right:
                RightButton_Click(this, null);
                e.Handled = true;
                break;
            case Key.Space:
                ProcessButton_Click(this, null);
                e.Handled = true; // prevent the focused button from being "clicked"
                break;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;

        _hotkeys = new HotkeyManager(handle); 
  
        _hotkeys.Register(Key.D, ModifierKeys.Control, async () =>
        {
            var tooltipImage = _screenshots!.CaptureItemTooltip();

            if (tooltipImage != null)
            {
                System.Media.SystemSounds.Asterisk.Play();

                //var text = await ocrService.ReadAsync(tooltipImage);
                //Dispatcher.Invoke(() => webView.ShowWithData(text));
            }
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _hotkeys?.Dispose();
        base.OnClosed(e);
    }

    private void BrowseImages_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select images",
            Filter = "Image Files|*.png;*.bmp",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            _imagePaths = dialog.FileNames.ToList();
            _currentImageIndex = 0;

            LoadCurrentImage();
          
        }

        this.Focus(); // window gets focus
    }

    private void LoadCurrentImage()
    {
        if (_currentImageIndex < 0 || _currentImageIndex >= _imagePaths.Count)
            return;

        var path = _imagePaths[_currentImageIndex];

        ImagePathTextBox.Text = path;

        _loadedImage = _screenshots.LoadScreenshotImage(path);

        ScreenshotImageControl.Source = _loadedImage;
    }

    private void LeftButton_Click(object sender, RoutedEventArgs e)
    {
        if(_currentImageIndex > 0)
        {
            _currentImageIndex--;
            LoadCurrentImage();
        }
    }

    private void RightButton_Click(object sender, RoutedEventArgs e)
    {
        if(_currentImageIndex < _imagePaths.Count - 1)
        {
            _currentImageIndex++;
            LoadCurrentImage();
        }
    }
    private void ProcessButton_Click(object sender, RoutedEventArgs e)
    {
        if (_loadedImage is null)
            return;

        _loadedBitmap = _screenshots.BitmapFromBitmapImage(_loadedImage);

        var tooltip = _screenshots.DetectTooltip(_loadedBitmap);

        if (tooltip != null)
        {
            TooltipImageControl.Source = _screenshots.BitmapImageFromBitmap(tooltip);
        }
    }
}