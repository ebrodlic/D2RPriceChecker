using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace D2RPriceChecker.Windows
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();

            // Set full screen on load
            Loaded += OnWindowLoaded;

            // Click anywhere on overlay window
            Root.MouseDown += OnBackgroundClicked;

            // Content panel stops click bubbling
            ContentPanel.MouseDown += (s, e) => e.Handled = true;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            IsHitTestVisible = true; // click-through background initially
        }

        private void OnBackgroundClicked(object sender, MouseButtonEventArgs e)
        {
            // If click came from content panel → ignore
            if (IsClickInsideContent(e))
                return;

            ClearFields();
            HideOverlay();
        }

        private void ClearFields()
        {
            OcrText.Text = string.Empty;
            PriceText.Text = string.Empty;
        }

        private bool IsClickInsideContent(MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            while (source != null)
            {
                if (source == ContentPanel)
                    return true;

                source = VisualTreeHelper.GetParent(source);
            }

            return false;
        }
        public void DisplayText(string text)
        {
            OcrText.Text = text;

            Visibility = Visibility.Visible;
            Activate(); // bring on top of game
        }


        // TODO - not sure about this for now
        public void DisplayPrices(List<string> prices)
        {
            PriceText.Text = string.Join("\n", prices);
        
            Visibility = Visibility.Visible;
            Activate(); // bring on top of game
        }

        public void HideOverlay()
        {
            Visibility = Visibility.Hidden;
        } 
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                HideOverlay();
        }

        private void ContentPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

    }
}
