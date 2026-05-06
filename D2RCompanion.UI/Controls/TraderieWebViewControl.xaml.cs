// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using D2RCompanion.UI.AppCore;
using D2RCompanion.UI.Traderie;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

namespace D2RCompanion.UI.Controls
{
    /// <summary>
    /// Interaction logic for TraderieWebViewControl.xaml
    /// </summary>
    public partial class TraderieWebViewControl : UserControl
    {
        private readonly string _homeUrl;
        private readonly string _userDataFolder;
        private readonly Dictionary<string, TaskCompletionSource<string>> _pendingFetches = new();
        private readonly ILogger _logger;
        public TraderieSession Session { get; private set; } = new();
        public bool IsLoggedIn => !string.IsNullOrEmpty(Session.Jwt) && !string.IsNullOrEmpty(Session.Jwt);

        public TraderieWebViewControl(
            IConfiguration config,
            ILogger<TraderieWebViewControl> logger,
            AppInfo appInfo)
        {
            _homeUrl = config.GetValue<string>("Traderie:HomeUrl");

            _userDataFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appInfo.Id,
                "Traderie");

            _logger = logger;

            Loaded += TraderieWebViewControlLoaded;
            InitializeComponent();
        }
        private async void TraderieWebViewControlLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Ensure WebView2 is initialized when the UserControl is loaded
            //await InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            if (!Directory.Exists(_userDataFolder))
                Directory.CreateDirectory(_userDataFolder);

            await TraderieWebView.Dispatcher.InvokeAsync(async () =>
            {
                var env = await CoreWebView2Environment.CreateAsync(
                     null,                // browser executable folder (null = default)
                     _userDataFolder,     // persistent storage path
                     null);               // additional options

                // Make sure CoreWebView2 is initialized
                await TraderieWebView.EnsureCoreWebView2Async(env);

                await EnsureLoadedAsync(TraderieWebView);

                await TraderieWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                    (function() {
                        const oldLog = console.log;
                        console.log = function(...args) {
                            window.chrome.webview.postMessage({ type: 'console', data: args });
                            oldLog.apply(console, args);
                        };
                    })();
                    ");

                TraderieWebView.WebMessageReceived += OnWebMessageReceived;

                // Navigate to Traderie login page
                TraderieWebView.CoreWebView2.Navigate(_homeUrl);
            });
        }

        private static Task EnsureLoadedAsync(FrameworkElement element)
        {
            if (element.IsLoaded)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource();

            RoutedEventHandler? handler = null;
            handler = (_, __) =>
            {
                element.Loaded -= handler;
                tcs.SetResult();
            };

            element.Loaded += handler;
            return tcs.Task;
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                if (msg == null || !msg.ContainsKey("type")) return;

                var type = msg["type"]?.ToString();

                switch (type)
                {
                    case "console":
                        Console.WriteLine(JsonSerializer.Serialize(msg["data"]));
                        break;

                    case "fetchResult":
                    case "fetchError":
                        HandleFetchResponse(msg, type);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebMessage error: {ex.Message}");

                _logger.LogError(ex.Message);
            }
        }


        public Task<string> RunFetchAsync(string url, bool requireToken)
        {
            var tcs = new TaskCompletionSource<string>();
            var id = Guid.NewGuid().ToString();

            _pendingFetches[id] = tcs;

            var script = requireToken
               ? BuildTokenFetchScript(url, id)
               : BuildSimpleFetchScript(url, id);

            TraderieWebView.CoreWebView2.ExecuteScriptAsync(script);

            return tcs.Task;
        }
        public async Task<string> ExecuteScriptAsync(string script)
        {
            // Ensure CoreWebView2 is ready
            if (TraderieWebView.CoreWebView2 == null)
                throw new InvalidOperationException("WebView not initialized yet");

            var result = await TraderieWebView.CoreWebView2.ExecuteScriptAsync(script);

            // JS string is returned as JSON literal, so deserialize safely
            // e.g., if JS returns "hello", result is "\"hello\""
            return JsonSerializer.Deserialize<string>(result);
        }

        private void HandleFetchResponse(Dictionary<string, object> msg, string type)
        {
            if (!msg.TryGetValue("id", out var idObj)) return;

            var id = idObj?.ToString();
            if (id == null || !_pendingFetches.TryGetValue(id, out var tcs)) return;

            if (type == "fetchResult")
                tcs.TrySetResult(msg["data"]?.ToString());
            else
                tcs.TrySetException(new Exception(msg["data"]?.ToString()));

            _pendingFetches.Remove(id);
        }

        public async Task TryLoadSessionAsync()
        {
            try
            {
                if (TraderieWebView.CoreWebView2 == null)
                    throw new InvalidOperationException("WebView not initialized yet");

                var script = @"
                    (() => {
                        const jwt = localStorage.getItem('jwt');
                        const user = localStorage.getItem('user');
                         return JSON.stringify({
                             jwt: jwt,
                             user: user
                         });
                    })();
                ";

                var result = await ExecuteScriptAsync(script);

                if (string.IsNullOrWhiteSpace(result))
                    return;

                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                Session.Jwt = root.GetProperty("jwt").GetString() ?? "";

                var userJson = root.GetProperty("user").GetString();

                if (!string.IsNullOrWhiteSpace(userJson))
                {
                    using var userDoc = JsonDocument.Parse(userJson);
                    Session.UserId = userDoc.RootElement.GetProperty("id").GetString() ?? "";
                }
            }
            catch
            {

            }
        }

        private string BuildSimpleFetchScript(string url, string id)
        {
            return $@"
                (async () => {{
                    try {{
                        const res = await fetch('{url}', {{
                            method: 'GET',
                            credentials: 'include'
                        }});

                        const text = await res.text();

                        window.chrome.webview.postMessage({{
                            type: 'fetchResult',
                            id: '{id}',
                            data: text
                        }});

                    }} catch (e) {{
                        window.chrome.webview.postMessage({{
                            type: 'fetchError',
                            id: '{id}',
                            data: e.toString()
                        }});
                    }}
                }})();
                ";
        }

        private string BuildTokenFetchScript(string url, string id)
        {
            return $@"
                (async () => {{
                    try {{
                        const token = localStorage.getItem('jwt');
                        if (!token) throw new Error('JWT not found');

                        const res = await fetch('{url}', {{
                            method: 'GET',
                            headers: {{
                                'Authorization': 'Bearer ' + token,
                                'Accept': 'application/json'
                            }},
                            credentials: 'include'
                        }});

                        const text = await res.text();

                        window.chrome.webview.postMessage({{
                            type: 'fetchResult',
                            id: '{id}',
                            data: text
                        }});

                    }} catch (e) {{
                        window.chrome.webview.postMessage({{
                            type: 'fetchError',
                            id: '{id}',
                            data: e.toString()
                        }});
                    }}
                }})();
                ";
        }
        //private void BackButton_Click(object sender, RoutedEventArgs e)
        //{ 
        //    TraderieWebView.Visibility = Visibility.Collapsed;
        //}

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if(Visibility == Visibility.Visible)
                {
                    Visibility = Visibility.Hidden;
                    e.Handled = true;
                }
            }
        }
    }
}
