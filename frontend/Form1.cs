using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace KeranjangHijau
{
    public partial class Form1 : Form
    {
        private readonly BackendBridge _backendBridge;

        public Form1()
        {
            InitializeComponent();
            _backendBridge = new BackendBridge();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await webView21.EnsureCoreWebView2Async();
                webView21.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                string htmlPath = Path.Combine(Application.StartupPath, "index.html");

                if (File.Exists(htmlPath))
                {
                    webView21.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
                }
                else
                {
                    MessageBox.Show(
                        $"Could not find index.html at:\n{htmlPath}\n\nMake sure 'Copy to Output Directory' is set to 'Copy if newer' in File Properties.",
                        "File Missing Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Inisialisasi WebView2 gagal: {ex.Message}", "WebView2 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string rawMessage = e.TryGetWebMessageAsString();
                if (string.IsNullOrWhiteSpace(rawMessage)) return;

                string response = await _backendBridge.HandleMessageAsync(rawMessage);
                webView21.CoreWebView2.PostWebMessageAsString(response);
            }
            catch (Exception ex)
            {
                string escaped = ex.Message.Replace("\"", "\\\"");
                webView21.CoreWebView2.PostWebMessageAsString($"{{\"success\":false,\"error\":\"{escaped}\"}}");
            }
        }
    }
}
