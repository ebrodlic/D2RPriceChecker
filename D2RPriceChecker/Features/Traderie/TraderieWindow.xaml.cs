using D2RPriceChecker.Features.Traderie;
using D2RPriceChecker.Pipelines;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace D2RPriceChecker.Features.Traderie
{
    /// <summary>
    /// Interaction logic for TraderieWindow.xaml
    /// </summary>
    public partial class TraderieWindow : Window
    {
        private readonly string _homeUrl = "https://traderie.com/diablo2resurrected";
        private string _userDataFolder = null!;
        public TraderieSession Session { get; private set; } = new();
        public bool IsLoggedIn => !string.IsNullOrEmpty(Session.Jwt) && !string.IsNullOrEmpty(Session.Jwt);

        private readonly Dictionary<string, TaskCompletionSource<string>> _pendingFetches = new();
        public TraderieWindow()
        {
            InitializeComponent();
            InitializeUserDir();
        }
        private void InitializeUserDir()
        {
            _userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "D2RPriceChecker",
                "Traderie");

            if (!Directory.Exists(_userDataFolder))
                Directory.CreateDirectory(_userDataFolder);
        }    

        public async Task InitializeAsync()
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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Prevent actual closing
            e.Cancel = true;

            // Just hide instead
            this.Hide();
        }
    }
}
