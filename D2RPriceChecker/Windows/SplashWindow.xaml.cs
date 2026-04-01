using D2RPriceChecker.Pipelines;
using D2RPriceChecker.Services;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;


namespace D2RPriceChecker.Windows
{
    public partial class SplashWindow : Window
    {

        private NotifyIcon _trayIcon = null!;
        private OverlayWindow _overlay = null!;
        private TraderieWindow _traderie = null!;

        private HotkeyManager _hotkeys = null!;
      
        // Services
        private readonly ScreenshotService _screenshots = new();
        private readonly OcrService _ocrService = new OcrService("Models/d2r_tooltip_crnn_best.onnx");

        // Flags
        private bool _isProcessing;

        public SplashWindow()
        {
            InitializeComponent();
            SetupTray();
            SetupOverlay();
            SetupTraderie();
        }

        private void SetupTraderie()
        {
            _traderie = new TraderieWindow();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SetupHotkeys();
        }

        private void SetupHotkeys()
        {
            var handle = new WindowInteropHelper(this).Handle;

            _hotkeys = new HotkeyManager(handle);

            _hotkeys.Register(Key.D, ModifierKeys.Control, HandlePipelineHotkey);
            _hotkeys.Register(Key.O, ModifierKeys.Control, HandleOverlayToggleHotkey);
        }

        private void SetupTray()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = new Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon-16x16.ico"));
            _trayIcon.Visible = true;
            _trayIcon.Text = "D2R Price Checker";

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => { this.Show(); });
            menu.Items.Add("Log In", null, (s, e) => { _traderie.Show(); });
            menu.Items.Add("Exit", null, (s, e) => { System.Windows.Application.Current.Shutdown(); });
            _trayIcon.ContextMenuStrip = menu;

            _trayIcon.DoubleClick += (s, e) => { this.Show(); };
        }

        private void SetupOverlay()
        {
            _overlay = new OverlayWindow();
           
            _overlay.Visibility = Visibility.Hidden;
            _overlay.ShowInTaskbar = false;

            //_overlay.Show();
            //_overlay.Owner = this;
            //_overlay.Hide();

        }

        private async void HandlePipelineHotkey()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            try
            {
                StartProcessing();

                var detectionResult = RunDetectionPipeline(timestamp);
                SavePipelineResultData(timestamp, detectionResult);

                if(!detectionResult.IsTooltipFound)
                    return;

                //TODO - fix this - no need for new settings object here at all
                var segmentationResult = RunSegmentationPipeline(detectionResult.Tooltip!);
                SavePipelineResultData(timestamp, segmentationResult);

                var ocrText = await RunOcrPipelineAsync(segmentationResult);

                _overlay.DisplayText(string.Join("\n", ocrText));
            }
            finally
            { 
                StopProcessing();
            }
        }

        private void ToggleOverlay()
        {
            if (_overlay.IsVisible)
            {
                _overlay.Hide();
            }
            else
            {
                if (_overlay.Owner == null)
                    _overlay.Owner = this;

                _overlay.WindowState = WindowState.Normal;
                _overlay.Topmost = true;

                _overlay.Show();
            }
          
        }
        private void HandleOverlayToggleHotkey()
        {
            ToggleOverlay();
        }

        private async Task<List<string>> RunOcrPipelineAsync(TooltipLineSegmetnationPipelineResult segmentationResult)
        {
            return await Task.Run(() =>
            {
                return _ocrService.PredictTextBatch(segmentationResult.TooltipLines);
            });
        }

        private TooltipDetectionPipelineResult RunDetectionPipeline(string timestamp)
        {
            var screenshot = _screenshots.CapturePrimaryScreen();
            var detectionResult = new TooltipDetectionPipeline().Run(screenshot);

            return detectionResult;
        }

        private TooltipLineSegmetnationPipelineResult RunSegmentationPipeline(Bitmap tooltip)
        {
            var settings = new TooltipLineSegmentationPipelineSettings();
            var segmentationResult = new TooltipLineSegmentationPipeline().Run(tooltip, settings);

            return segmentationResult;
        }

        private void StartProcessing()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
        }

        private void StopProcessing()
        {
            if (_isProcessing)
            {
                _isProcessing = false;
            }
        }

        private void SavePipelineResultData(string timestamp, TooltipDetectionPipelineResult result)
        {
            var datasetManager = ((App)System.Windows.Application.Current).Cache;

            datasetManager.Save(timestamp, result);
        }
        private void SavePipelineResultData(string timestamp, TooltipLineSegmetnationPipelineResult result)
        {
            var datasetManager = ((App)System.Windows.Application.Current).Cache;

            datasetManager.Save(timestamp, result);
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only start drag on left mouse button
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Instead of closing the window, just hide it, sends to tray
            this.Hide();

            // Optional: show balloon tip from tray
            if (_trayIcon != null)
            {
                //_trayIcon.ShowBalloonTip(1000, "D2R Price Checker", "Application minimized to tray", ToolTipIcon.Info);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown(); // ensures full exit
        }

        public void Cleanup()
        {
            _trayIcon?.Dispose();
            _overlay?.Close();
        }

        private void MinimizeToTray()
        {
            this.Hide();
        }
    }
}
