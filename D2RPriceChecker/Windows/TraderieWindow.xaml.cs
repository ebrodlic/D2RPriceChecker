using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for TraderieWindow.xaml
    /// </summary>
    public partial class TraderieWindow : Window
    {
        public TraderieWindow()
        {
            InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            // Set persistent user data folder
            var userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "D2RPriceChecker",
                "Traderie");

            if (!Directory.Exists(userDataFolder))
                Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(
                 null,                // browser executable folder (null = default)
                 userDataFolder,      // persistent storage path
                 null);               // additional options

            // Make sure CoreWebView2 is initialized
            await TraderieWebView.EnsureCoreWebView2Async(env);

            // Navigate to Traderie login page
            TraderieWebView.CoreWebView2.Navigate("https://traderie.com/diablo2resurrected");
        }

        public CoreWebView2 CoreWebView2Instance => TraderieWebView.CoreWebView2;
    }
}
