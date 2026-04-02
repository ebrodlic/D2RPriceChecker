using D2RPriceChecker.Pipelines;
using D2RPriceChecker.Services;
using D2RPriceChecker.Util;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;


namespace D2RPriceChecker.Windows
{
    public partial class SplashWindow : Window
    {
        // Icons and Tray
        private NotifyIcon _trayIcon = null!;

        // Windows
        private OverlayWindow _overlay = null!;
        private TraderieWindow _traderie = null!;

        //Managers
        private HotkeyManager _hotkeys = null!;
      
        // Services
        private readonly ScreenshotService _screenshots = new();
        private OcrService _ocrService = null!;

        // Flags
        private bool _isProcessing;

        public SplashWindow()
        {
            InitializeComponent();
            SetVersion();
            SetupTray();
            SetupWindows();

            Loaded += OnWindowLoaded;
        }   

        private void SetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            VersionText.Text = $"v{version}";
        }
        private void SetupTray()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = new Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "icon-16x16.ico"));
            _trayIcon.Visible = true;
            _trayIcon.Text = "D2R Price Checker";

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => { 
                this.Show();
                //WindowState = WindowState.Normal;
                //Topmost = true;
                //this.Activate();
            });
            menu.Items.Add("Show Browser", null, (s, e) => { _traderie.Show(); });
            menu.Items.Add("Exit", null, (s, e) => { System.Windows.Application.Current.Shutdown(); });

            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (s, e) => { this.Show(); };
        }
        private void SetupWindows()
        {
            _overlay = new OverlayWindow();

            _overlay.Visibility = Visibility.Hidden;
            _overlay.ShowInTaskbar = false;

            //_overlay.Show();
            //_overlay.Owner = this;
            //_overlay.Hide();

            _traderie = new TraderieWindow();
            _traderie.Visibility = Visibility.Hidden;
            _traderie.ShowInTaskbar = false;



            //_traderie.Left = -10000;
            //_traderie.Top = -10000;

            _traderie.Show();
            //_traderie.Owner = this;
            _traderie.Hide();

        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // 1. Initialize OCR model async
            var ocrTask = InitializeOcrAsync();

            // 2. Initialize Traderie WebView async
            var traderieTask = _traderie.InitializeAsync();

            // 3. Wait for both
            await Task.WhenAll(ocrTask, traderieTask);

            // 4. Try to obtain session info for future use
            await _traderie.TryLoadSessionAsync();

            // 5. If no session info, show the traderie window so user can log in for session data
            if (!_traderie.IsLoggedIn)
                _traderie.Show(); 

            // 4. Now all resources are ready, safe to setup hotkeys
            SetupHotkeys();
        }

        private async Task InitializeOcrAsync() 
        {
            _ocrService = await Task.Run(() => new OcrService("Models/d2r_tooltip_crnn_best.onnx"));
        }

        private async Task InitializeTraderieAsync()
        {
            //_traderie.Left = -10000;
            //_traderie.Top = -10000;
           // _traderie.Show(); // gives WebView2 a real HWND
           // _traderie.Hide();
            await _traderie.InitializeAsync();
        }

        //private async Task SetupTraderieAsync()
        //{
        //    try
        //    {
        //        await _traderie.InitializeAsync();

        //    }
        //    catch (Exception ex)
        //    {

        //    }




        //    //_traderieWindow = new TraderieWindow();
        //    //_traderieWindow.Show();  // Show the window to initialize WebView2, then hide it immediately
        //    //_traderieWindow.Hide();
        //}

        private void SetupTraderie()
        {
            _traderie = new TraderieWindow();
            _traderie.Show();  // Show the window to initialize WebView2, then hide it immediately
            _traderie.Hide();
        }

        private void SetupHotkeys()
        {
            var handle = new WindowInteropHelper(this).Handle;

            _hotkeys = new HotkeyManager(handle);

            _hotkeys.Register(Key.D, ModifierKeys.Control, HandlePipelineHotkey);
            _hotkeys.Register(Key.O, ModifierKeys.Control, HandleOverlayToggleHotkey);
        }


     

        private async void HandlePipelineHotkey()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            try
            {
                StartProcessing();

                var detectionResult = RunDetectionPipeline(timestamp);
                SavePipelineResultData(timestamp, detectionResult);

                if(!detectionResult.IsTooltipFound())
                    return;

                //TODO - fix this - no need for new settings object here at all
                var segmentationResult = RunSegmentationPipeline(detectionResult.Tooltip!);
                SavePipelineResultData(timestamp, segmentationResult);

                var itemMetadata = new ItemDetectionPipeline().Run(segmentationResult.TooltipLines[0]);
                var itemText = await RunOcrPipelineAsync(segmentationResult);

                //_overlay.Show();
                _overlay.ShowOverlay();
                _overlay.UpdateValues(itemText);

                //TODO - not a fan of traderie window being called to do compute stuff
                var completedOffers = await _traderie.GetPriceData(itemMetadata, itemText);

                var trades = OffersParser.ParseOffers(completedOffers);

                _overlay.UpdateValues(trades);


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
