using D2RPriceChecker.Pipelines;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace D2RPriceChecker.Windows
{
    /// <summary>
    /// Interaction logic for TraderieWindow.xaml
    /// </summary>
    public partial class TraderieWindow : Window
    {
        private readonly Dictionary<string, TaskCompletionSource<string>> _pendingFetches = new();

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

            // Make sure the control is loaded in the visual tree
            if (!TraderieWebView.IsLoaded)
            {
                var tcs = new TaskCompletionSource();
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (s, e) =>
                {
                    TraderieWebView.Loaded -= loadedHandler;
                    tcs.SetResult();
                };
                TraderieWebView.Loaded += loadedHandler;
                await tcs.Task;
            }

            await TraderieWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                (function() {
                    const oldLog = console.log;
                    console.log = function(...args) {
                        window.chrome.webview.postMessage({ type: 'console', data: args });
                        oldLog.apply(console, args);
                    };
                })();
                ");

            TraderieWebView.WebMessageReceived += WebView_WebMessageReceived;

            // Navigate to Traderie login page
            TraderieWebView.CoreWebView2.Navigate("https://traderie.com/diablo2resurrected");
        }

        public CoreWebView2 CoreWebView2Instance => TraderieWebView.CoreWebView2;

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);

                if (msg != null && msg.ContainsKey("type"))
                {
                    var type = msg["type"].ToString();

                    if (type == "console")
                    {
                        // Log console messages from JS
                        Console.WriteLine("JS Console: " + JsonSerializer.Serialize(msg["data"]));
                    }
                    else if ((type == "fetchResult" || type == "fetchError") && msg.ContainsKey("id"))
                    {
                        var id = msg["id"].ToString();
                        if (_pendingFetches.TryGetValue(id, out var tcs))
                        {
                            if (type == "fetchResult")
                                tcs.TrySetResult(msg["data"].ToString());
                            else
                                tcs.TrySetException(new Exception(msg["data"].ToString()));

                            _pendingFetches.Remove(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WebMessageReceived parse failed: " + ex.Message);
            }
        }

        // TODO not sure here, format result text, or just return json for webview to render
        public async Task<string> GetPriceData(ItemMetadata metadata, List<string> text)
        {
            var result = string.Empty;

            try
            {
                if (metadata.Rarity == ItemRarity.Unique || metadata.Rarity == ItemRarity.Set)
                {
                    var itemName = text[0].Trim();
                    var encoded = Uri.EscapeDataString(itemName);
                    var searchUrl = $"https://traderie.com/api/diablo2resurrected/items?variants=&search={encoded}&tags=true";

                    var searchJson = await FetchAsync(searchUrl);


                    using var searchDoc = JsonDocument.Parse(searchJson);

                    var items = searchDoc.RootElement.GetProperty("items");
                    if (items.GetArrayLength() == 0)
                        return null;

                    var itemId = items[0].GetProperty("id").GetString();
                    var itemSlug = items[0].GetProperty("slug").GetString();

                 //   string productPageUrl = $"https://traderie.com/diablo2resurrected/product/{itemSlug}?prop_Ladder=true&prop_Game%20version=reign%20of%20the%20warlock";

               //     await ExecuteScriptAsync($@"window.location.href = '{productPageUrl}';");

                    // Wait for some time to let the page JS load
                 //   await Task.Delay(5000); // 2 seconds, adjust as needed

                   // string recentUrl = $"https://traderie.com/diablo2resurrected/product/maras-kaleidoscope/recent?prop_Ladder=true&prop_Game%20version=reign%20of%20the%20warlock";

                    //await ExecuteScriptAsync($@"window.location.href = '{recentUrl}';");

                    //await Task.Delay(5000); // 2 seconds, adjust as needed


                    // Step 2: Get completed offers
                    var offersUrl = $"https://traderie.com/api/diablo2resurrected/offers?accepted=true&currBuyer=1149841100&properties=true&completed=true&item={itemId}&prop_Ladder=true&prop_Game%20version=reign%20of%20the%20warlock";
                    var offersJson = await FetchAsyncWithToken(offersUrl);

                    //using var pricesDoc = JsonDocument.Parse(offersJson);

                    //var prices = new List<string>();

                    //var offers = pricesDoc.RootElement.GetProperty("offers");
                    //if(offers.GetArrayLength() == 0)
                    //    return null;

         

                    return offersJson;

                }
                else
                {
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error in GetPriceData: " + ex.Message);

            }           

            return result;
        }


        public Task<string> FetchAsyncWithToken(string url)
        {
            var tcs = new TaskCompletionSource<string>();
            var id = Guid.NewGuid().ToString();
            _pendingFetches[id] = tcs;

            // JS code to run in the WebView
            var script = $@"
                (async () => {{
                    try {{
                        const token = localStorage.getItem('jwt');
                        if (!token) throw new Error('JWT token not found');

                        const res = await fetch('{url}', {{
                            method: 'GET',
                            headers: {{
                                'Authorization': 'Bearer ' + token,
                                'Accept': 'application/json'
                            }},
                            credentials: 'include'
                        }});

                        const text = await res.text();

                        // Send result back to C# via postMessage
                        window.chrome.webview.postMessage({{
                            type: 'fetchResult',
                            data: text,
                            id: '{id}'
                        }});
                    }} catch(e) {{
                        // Send error back to C# via postMessage
                        window.chrome.webview.postMessage({{
                            type: 'fetchError',
                            data: e.toString(),
                            id: '{id}'
                        }});
                    }}
                }})();
            ";

            TraderieWebView.CoreWebView2.ExecuteScriptAsync(script);
            return tcs.Task;
        }

        public Task<string> FetchAsync(string url)
        {
            var tcs = new TaskCompletionSource<string>();
            var id = Guid.NewGuid().ToString();
            _pendingFetches[id] = tcs;

            // Inject JS that includes the ID in postMessage
            var script = $@"
                (async () => {{
                    try {{
                        const res = await fetch('{url}', {{
                            method: 'GET',
                            credentials: 'include'
                        }});
                        const text = await res.text();
                        window.chrome.webview.postMessage({{ type: 'fetchResult', data: text, id: '{id}' }});
                    }} catch(err) {{
                        window.chrome.webview.postMessage({{ type: 'fetchError', data: err.toString(), id: '{id}' }});
                    }}
                }})();
            ";

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

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Prevent actual closing
            e.Cancel = true;

            // Just hide instead
            this.Hide();
        }
    }
}
