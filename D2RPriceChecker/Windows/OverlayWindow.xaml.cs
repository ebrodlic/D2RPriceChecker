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

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
            Topmost = true;

            Loaded += (s, e) =>
            {
                Left = 0;
                Top = 0;
                Width = SystemParameters.PrimaryScreenWidth;
                Height = SystemParameters.PrimaryScreenHeight;
                IsHitTestVisible = true; // click-through background initially
            };

            // Click anywhere on overlay window
            Root.MouseDown += OnBackgroundClicked;

            // Content panel stops click bubbling
            ContentPanel.MouseDown += (s, e) => e.Handled = true;
        }

        private void OnBackgroundClicked(object sender, MouseButtonEventArgs e)
        {
            // If click came from content panel → ignore
            if (IsClickInsideContent(e))
                return;

            HideOverlay();
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
        public void ShowResult(string text)
        {
            OcrText.Text = text;

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
