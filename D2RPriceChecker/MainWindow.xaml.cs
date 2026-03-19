using D2RPriceChecker.Services;
using Microsoft.Win32;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;

namespace D2RPriceChecker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // State
    private List<string> _imagePaths = new();
    private int _currentImageIndex = -1;
    private BitmapImage? _loadedImage;
    private Bitmap? _loadedBitmap;

    // Variables for drag-to-scroll
    private System.Windows.Point _startPoint;
    private double _startHorizontalOffset;
    private double _startVerticalOffset;
    private bool _isDragging = false;

    // Services
    private readonly ScreenshotService _screenshots = new();
    private HotkeyManager? _hotkeys;

    // Flags
    private bool _isProcessing;

    public MainWindow()
    {
        InitializeComponent();

        // Register key event handler for navigation and processing
        this.PreviewKeyDown += MainWindow_PreviewKeyDown;

        // Load the spinner GIF once
        var spinnerUri = new Uri("pack://application:,,,/Resources/spinner.gif");
        var spinnerImage = new BitmapImage(spinnerUri);
        ImageBehavior.SetAnimatedSource(LoadingSpinner, spinnerImage);
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
    }

    private void Scroll_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer == null) return;

        _isDragging = true;
        _startPoint = e.GetPosition(this);
        _startHorizontalOffset = scrollViewer.HorizontalOffset;
        _startVerticalOffset = scrollViewer.VerticalOffset;

        scrollViewer.Cursor = Cursors.SizeAll;
        scrollViewer.CaptureMouse();
    }

    private void Scroll_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer == null) return;

        System.Windows.Point currentPoint = e.GetPosition(this);
        Vector delta = currentPoint - _startPoint;

        scrollViewer.ScrollToHorizontalOffset(_startHorizontalOffset - delta.X);
        scrollViewer.ScrollToVerticalOffset(_startVerticalOffset - delta.Y);
    }

    private void Scroll_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer == null) return;

        _isDragging = false;
        scrollViewer.Cursor = Cursors.Arrow;
        scrollViewer.ReleaseMouseCapture();
    }

    private void LoadCurrentImage()
    {
        if (_currentImageIndex < 0 || _currentImageIndex >= _imagePaths.Count)
            return;

        var path = _imagePaths[_currentImageIndex];

        ImagePathTextBox.Text = path;

        _loadedImage = _screenshots.LoadScreenshotImage(path);

        Stage1Image.Source = _loadedImage;

        if (AutoProcessCheckBox.IsChecked == true)
            ProcessButton_Click(this, null);
    }

    private void LeftButton_Click(object sender, RoutedEventArgs? e)
    {
        if(_currentImageIndex > 0)
        {
            _currentImageIndex--;
            LoadCurrentImage();
        }
    }

    private void RightButton_Click(object sender, RoutedEventArgs? e)
    {
        if(_currentImageIndex < _imagePaths.Count - 1)
        {
            _currentImageIndex++;
            LoadCurrentImage();
        }
    }
    private async void ProcessButton_Click(object sender, RoutedEventArgs? e)
    {
        if (_loadedImage is null || _isProcessing)
            return;

        _isProcessing = true;
        try
        {
            LoadingSpinner.Visibility = Visibility.Visible;

            _loadedBitmap = await Task.Run(() => _screenshots.BitmapFromBitmapImage(_loadedImage));

            // Stage 1: Original screenshot (already shown in Stage1Image via LoadCurrentImage)

            // Stage 2: Border mask — white = gray-matching pixels, black = everything else
            var mask = await Task.Run(() => _screenshots.DebugBorderMask(_loadedBitmap));
            Stage2Image.Source = _screenshots.BitmapImageFromBitmap(mask);

            // Stage 3: Connected components — each blob gets a distinct color
            var components = await Task.Run(() => _screenshots.DebugComponents(_loadedBitmap));
            Stage3Image.Source = _screenshots.BitmapImageFromBitmap(components);

            // Stage 4: Rectangular border detection — red rectangle overlay on original
            var border = await Task.Run(() => _screenshots.DebugRectangularBorder(_loadedBitmap));
            Stage4Image.Source = _screenshots.BitmapImageFromBitmap(border);

            // Final result: full pipeline (trim + separator + crop)
            var tooltip = await Task.Run(() => _screenshots.DetectTooltip(_loadedBitmap));
            TooltipImageControl.Source = tooltip != null
                ? _screenshots.BitmapImageFromBitmap(tooltip)
                : null;
        }
        finally
        {
            _isProcessing = false;
            LoadingSpinner.Visibility = Visibility.Collapsed;
        }
    }
}