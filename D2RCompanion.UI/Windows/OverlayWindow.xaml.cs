using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.Messaging;
using D2RCompanion.Core.Traderie.Domain;
using D2RCompanion.Core.Traderie.DTO;
using D2RCompanion.UI.Controls;
using D2RCompanion.UI.Messages;
using D2RCompanion.UI.Services;
using D2RCompanion.UI.ViewModels;
using D2RCompanion.UI.Views;
using Microsoft.Extensions.Logging;

namespace D2RCompanion.UI.Windows
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private readonly HotkeyService _hotkeys;
        private readonly TraderieWebViewControl _traderieWebView;

        private readonly ILogger _logger;

        public OverlayWindow(
            OverlayViewModel vm,
            HotkeyService hotkeys,
            TraderieWebViewControl traderieWebView,
            ILogger<OverlayWindow> logger
            )
        {
            _hotkeys = hotkeys;
            _traderieWebView = traderieWebView;
            _logger = logger;

            Loaded += OnWindowLoaded;
            InitializeComponent();

            DataContext = vm;
        }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            SetFullscreen();
            SetHandlers();
            SetMessageHandlers();
            SetHotkeys();

            CreateTraderieWebViewControl();
        }

        public void HideOverlayContent()
        {
            Root.Opacity = 0;
            Root.IsHitTestVisible = false;
        }

        public void ShowOverlayContent()
        {
            Root.Opacity = 1;
            Root.IsHitTestVisible = true;
            Topmost = true;

            Activate();
        }

        private void SetMessageHandlers()
        {
            WeakReferenceMessenger.Default.Register<OverlayVisibilityRequestMessage>(this, (r, m) =>
            {
                if (m.IsVisible)
                {
                    ShowOverlayContent();
                }
                else
                {
                    HideOverlayContent();
                }


                //if (m.IsVisible)
                //{
                //    Show();
                //}
                //else
                //{
                //    Hide();
                //}
            });

            WeakReferenceMessenger.Default.Register<TraderieVisibilityRequestMessage>(this, (r, m) =>
            {
                if (m.Toggle)
                { 
                    _traderieWebView.Visibility = _traderieWebView.IsVisible ? Visibility.Hidden : Visibility.Visible;
                }
                else
                {
                    _traderieWebView.Visibility = m.Visibility ? Visibility.Visible : Visibility.Hidden;
                }
            });
        }

        private async Task CreateTraderieWebViewControl()
        {
            Grid.SetColumn(_traderieWebView, 1);

            Root.Children.Add(_traderieWebView);

            // Set to visible to trigger the inital webview load
            _traderieWebView.Visibility = Visibility.Visible;

            await _traderieWebView.InitializeAsync();
            await Task.Delay(300);
            await _traderieWebView.TryLoadSessionAsync();
            await Task.Delay(300);

            if (_traderieWebView.IsLoggedIn)
            {
                _traderieWebView.Visibility = Visibility.Hidden;
            }
        }

        private void SetFullscreen()
        {
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
        }

        private void SetHandlers()
        {
            // Click anywhere on overlay window
            Root.MouseDown += OnBackgroundClicked;
        }

        private void SetHotkeys()
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            _hotkeys.Initialize(hwnd);
            _hotkeys.Register(Key.Space, ModifierKeys.Shift, ToggleVisibility);

            _hotkeys.Register(Key.D, ModifierKeys.Control, () =>
            {
                _logger.LogDebug("Ctrl+D pressed");

                _ = ((OverlayViewModel)DataContext).InitiatePriceCheck();
            });
        }

        private void CheckTraderieStatus()
        {
            //TraderieStatus.Text = _traderieWebView.IsLoggedIn ? "Logged In" : "NOT Logged In";
          
        }

        private void OnCheckStatusClick(object sender, RoutedEventArgs e)
        {
            CheckTraderieStatus();
        }

        private async void ToggleVisibility()
        {
            if (Root.Opacity == 0)
            {
                ShowOverlayContent();

            }
            else
            {
                HideOverlayContent();
            }


            //if (Visibility == Visibility.Visible)
            //    Hide();
            //else
            //{
            //    Show();
            //}
        }

        private void OnBackgroundClicked(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == Root)
            {
                HideOverlayContent();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                HideOverlayContent();
        }
    }
}
