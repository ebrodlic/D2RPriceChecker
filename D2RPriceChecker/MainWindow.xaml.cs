using D2RPriceChecker.Services;
using D2RPriceChecker.Util;
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
    private Bitmap? _loadedBitmap;

    // Variables for drag-to-scroll
    private System.Windows.Point _startPoint;
    private double _startHorizontalOffset;
    private double _startVerticalOffset;
    private bool _isDragging = false;

    // Services
    private readonly ScreenshotService _screenshots = new();
    private readonly TooltipPipeline _detection = new(); //get rid of this?
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

        _hotkeys.Register(Key.D, ModifierKeys.Control, HandlePipelineHotkey);
    }
  
    protected override void OnClosed(EventArgs e)
    {
        _hotkeys?.Dispose();
        base.OnClosed(e);
    }



    private async void HandlePipelineHotkey()
    {
        try
        {
            StartProcessing();
            ClearImageFields();

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            var screenshot = _screenshots.CapturePrimaryScreen();
            var result = _detection.Run(screenshot);
         
            PopulateImageFields(result);
            SavePipelineResultData(timestamp, result);

            if (result.IsTooltipFound)
            {
                //var text = await ocrService.ReadAsync(tooltipImage);
                //Dispatcher.Invoke(() => webView.ShowWithData(text));
            }
        }
        finally
        {
            StopProcessing();
        }

     
        //mb do timestamp here






        // Save the captured screenshot and results for debugging to disk
    }

    private void SavePipelineResultData(string timestamp, TooltipPipelineResult result)
    {
        var datasetManager = ((App)Application.Current).DatasetManager;

        datasetManager.Save(timestamp, result);
    }

    private void ClearImageFields()
    {
        if (Application.Current.MainWindow?.IsVisible == false)
            return;

        Stage1Image.Source = null;
        Stage2Image.Source = null;
        Stage3Image.Source = null;
        Stage4Image.Source = null;
        TooltipImageControl.Source = null;
    }   

    private void PopulateImageFields(TooltipPipelineResult result, bool includeScreenshotField = true)
    {
        if (Application.Current.MainWindow?.IsVisible == false)
            return;

        // Stage 1: Original screenshot
        if(includeScreenshotField)
            Stage1Image.Source = BitmapConverter.BitmapImageFromBitmap(result.Screenshot);

        // Stage 2: Border mask — white = gray-matching pixels, black = everything else
        if (result.BorderMask != null)
            Stage2Image.Source = BitmapConverter.BitmapImageFromBitmap(result.BorderMask);

        // Stage 3: Connected components — each blob gets a distinct color
        if (result.Components != null)
            Stage3Image.Source = BitmapConverter.BitmapImageFromBitmap(result.Components);

        // Stage 4: Rectangular border detection — red rectangle overlay on original
        if (result.BorderOverlay != null)
            Stage4Image.Source = BitmapConverter.BitmapImageFromBitmap(result.BorderOverlay);

        // Final result: cropped tooltip
        if (result.Tooltip != null)
            TooltipImageControl.Source = BitmapConverter.BitmapImageFromBitmap(result.Tooltip);
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

        _loadedBitmap = _screenshots.LoadBitmapSafe(path);

        Stage1Image.Source = BitmapConverter.BitmapImageFromBitmap(_loadedBitmap);

        if (AutoProcessCheckBox.IsChecked == true)
            ProcessButton_Click(this, null);
    }

    private void LeftButton_Click(object sender, RoutedEventArgs? e)
    {
        if(_currentImageIndex > 0 && !_isProcessing)
        {
            _currentImageIndex--;
            LoadCurrentImage();
        }
    }

    private void RightButton_Click(object sender, RoutedEventArgs? e)
    {
        if(_currentImageIndex < _imagePaths.Count - 1 && !_isProcessing)
        {
            _currentImageIndex++;
            LoadCurrentImage();
        }
    }

    private async void ProcessButton_Click(object sender, RoutedEventArgs? e)
    {
        if (_loadedBitmap is null)
            return;

        try
        {
            StartProcessing();

            // Run the full pipeline once
            var result = await Task.Run(() => _detection.Run(_loadedBitmap));

            PopulateImageFields(result);
        }
        finally
        {
            StopProcessing();
        }
    }

    private void StartProcessing()
    {
        if (_isProcessing)
            return;

        _isProcessing = true;
        LoadingSpinner.Visibility = Visibility.Visible;
    }

    private void StopProcessing()
    {
        if (_isProcessing)
        {
            _isProcessing = false;
            LoadingSpinner.Visibility = Visibility.Collapsed;
        }
    }


}