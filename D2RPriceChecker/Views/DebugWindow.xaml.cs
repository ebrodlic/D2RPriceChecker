using D2RPriceChecker.Pipelines;
using D2RPriceChecker.Services;
using D2RPriceChecker.Util;
using Microsoft.Win32;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfAnimatedGif;

namespace D2RPriceChecker.UI.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class DebugWindow : Window
{
    // State
    private List<string> _imagePaths = new();
    private string _currentImagePath = string.Empty;
    private int _currentImageIndex = -1;
    private Bitmap? _loadedBitmap;

    // Variables for drag-to-scroll
    private System.Windows.Point _startPoint;
    private double _startHorizontalOffset;
    private double _startVerticalOffset;
    private bool _isDragging = false;

    // Services
    private readonly ScreenshotService _screenshots = new();
    private readonly TooltipDetectionPipeline _detection = new(); //get rid of this?
    private readonly TooltipLineSegmentationPipeline _segmentation = new(); //this too?
    private HotkeyManager? _hotkeys;

    // Flags
    private bool _isProcessing;

    public DebugWindow()
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
        _hotkeys.Register(Key.O, ModifierKeys.Control, HandleOverlayToggleHotkey);
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

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var screenshot = _screenshots.CapturePrimaryScreen();
            var detectionResult = _detection.Run(screenshot);
         
            PopulateImageFields(detectionResult);
            SavePipelineResultData(timestamp, detectionResult);

            if (detectionResult.IsTooltipFound())
            {
                //TODO - fix this - no need for new settings object here at all
                var segmentationResult = _segmentation.Run(detectionResult.Tooltip, new TooltipLineSegmentationPipelineSettings());

                DebugLogTextBox.Text = segmentationResult.TooltipLines.Count.ToString();

                PopulateSegmentationImageFields(segmentationResult);
                SavePipelineResultData(timestamp, segmentationResult);

                // Clear previous OCR results
                OcrResultTextBox.Text = string.Empty;

                var text = await Task.Run(() => new OcrService("Models/d2r_tooltip_crnn_best.onnx").PredictTextBatch(segmentationResult.TooltipLines));

                foreach (var line in text)
                {
                    OcrResultTextBox.Text += line + "\n";
                }

                //foreach (var val in text)
                //{
                //    OcrResultTextBox.Text += val;
                //}

                //var text = await ocrService.ReadAsync(tooltipImage);
                //Dispatcher.Invoke(() => webView.ShowWithData(text));
            }
        }
        finally
        {
            StopProcessing();
        }





        // Save the captured screenshot and results for debugging to disk
    }

    private void HandleOverlayToggleHotkey()
    {
        throw new NotImplementedException();
    }

    private void PopulateSegmentationImageFields(TooltipLineSegmetnationPipelineResult segmentationResult)
    {
        if (Application.Current.MainWindow?.IsVisible == false)
            return;

        LinesStackPanel.Children.Clear();

        foreach (var bitmap in segmentationResult.TooltipLines)
        {
            var lineImage = BitmapConverter.BitmapImageFromBitmap(bitmap);

            System.Windows.Controls.Image img = new System.Windows.Controls.Image
            {
                Source = lineImage,
                Margin = new Thickness(5),
            };

            LinesStackPanel.Children.Add(img);
        }
    }

    private void SavePipelineResultData(string timestamp, TooltipDetectionPipelineResult result)
    {
        var datasetManager = ((App)Application.Current).Cache;

        datasetManager.Save(timestamp, result);
    }
    private void SavePipelineResultData(string timestamp, TooltipLineSegmetnationPipelineResult result)
    {
        var datasetManager = ((App)Application.Current).Cache;

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

    private void PopulateImageFields(TooltipDetectionPipelineResult result, bool includeScreenshotField = true)
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

    private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Image img)
        {
            if(img != null)
            {
                OverlayImage.Source = img.Source;
                Overlay.Visibility = Visibility.Visible;
            }
        }
    }

    private void Overlay_Close(object sender, MouseButtonEventArgs e)
    {
        Overlay.Visibility = Visibility.Collapsed;
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

    private void LoadCurrentImage()
    {
        if (_currentImageIndex < 0 || _currentImageIndex >= _imagePaths.Count)
            return;

        _currentImagePath = _imagePaths[_currentImageIndex];

        ImagePathTextBox.Text = _currentImagePath;

        _loadedBitmap = _screenshots.LoadBitmapSafe(_currentImagePath);

        Stage1Image.Source = BitmapConverter.BitmapImageFromBitmap(_loadedBitmap);

        if (AutoProcessCheckBox.IsChecked == true)
            ProcessButton_Click(this, null);
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

            //var timestamp = Path.GetFileNameWithoutExtension(_currentImagePath);
            
            var detectionResult = await Task.Run(() => _detection.Run(_loadedBitmap));

            PopulateImageFields(detectionResult);            
            //SavePipelineResultData(timestamp, detectionResult);

            var segmentationSettings = GetSettingsFromUI();
            var segmentationResult = _segmentation.Run(detectionResult.Tooltip, segmentationSettings);

            DebugLogTextBox.Text = segmentationResult.TooltipLines.Count.ToString();

            PopulateSegmentationImageFields(segmentationResult);
            //SavePipelineResultData(timestamp, segmentationResult);

            // Clear previous OCR results
            OcrResultTextBox.Text = string.Empty;

            var text = await Task.Run(() => new OcrService("Models/d2r_tooltip_crnn_best.onnx").PredictTextBatch(segmentationResult.TooltipLines));

            foreach (var line in text)
            {
                OcrResultTextBox.Text += line + "\n";
            }
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

    //// TODO - GENERATED - inspect later
    private TooltipLineSegmentationPipelineSettings GetSettingsFromUI()
    {
        var settings = new TooltipLineSegmentationPipelineSettings();

        if (int.TryParse(ContentCutoffValueTextBox.Text, out int cutoff))
            settings.ContentCutoffValue = cutoff;

        if (int.TryParse(DistanceThresholdTextBox.Text, out int distance))
            settings.DistanceThreshold = distance;

        if (int.TryParse(MaxRowsBlobSequenceLengthTextBox.Text, out int maxRows))
            settings.MaxRowsBlobSequenceLength = maxRows;

        if (int.TryParse(PlaceholderPropTextBox.Text, out int placeholder))
            settings.PlaceholderProp = placeholder;

        if (int.TryParse(BrightnessThresholdTextBox.Text, out int brightness))
            settings.BrightnessThreshold = brightness;

        if (int.TryParse(MaxChannelThresholdTextBox.Text, out int maxChannel))
            settings.MaxChannelThreshold = maxChannel;

        if (int.TryParse(CapitalizationOffsetTextBox.Text, out int capOffset))
            settings.CapitalizationOffset = capOffset;

        if (int.TryParse(FloorOffsetTextBox.Text, out int floorOffset))
            settings.FloorOffset = floorOffset;

        if (int.TryParse(PaddingTopTextBox.Text, out int paddingTop))
            settings.PaddingTop = paddingTop;

        if (int.TryParse(PaddingBottomTextBox.Text, out int paddingBottom))
            settings.PaddingBottom = paddingBottom;

        if (int.TryParse(PaddingLeftTextBox.Text, out int paddingLeft))
            settings.PaddingLeft = paddingLeft;

        if (int.TryParse(PaddingRightTextBox.Text, out int paddingRight))
            settings.PaddingRight = paddingRight;

        return settings;
    }


}