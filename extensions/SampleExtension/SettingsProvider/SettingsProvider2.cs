// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using Serilog;

namespace SampleExtension.Providers;

public class SettingsProvider2 : ISettingsProvider2
{
    private static readonly Lazy<ILogger> _logger = new(() => Serilog.Log.ForContext("SourceContext", nameof(SettingsProvider2)));

    private static readonly ILogger _log = _logger.Value;
    private readonly string _url;
    private readonly string _webContentPath = Path.Combine(AppContext.BaseDirectory, "WebContent");
    private WebServer.WebServer? _extensionWebServer;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public DisplayType DisplayType => DisplayType.WebView2;

    public SettingsProvider2()
    {
        // select a method to get the URL
        // _url = GetUrlFromFilePath("ExtensionSettingsPage.html");

        // use the web server to serve the page
        _url = GetUrlFromWebServer("ExtensionSettingsPage.html");

        // use a public website
        // _url = "https://github.com/login";
    }

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");

        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public WebViewResult GetWebView()
    {
        if (!string.IsNullOrEmpty(_url))
        {
            return new WebViewResult(_url);
        }

        return new WebViewResult(new NotImplementedException(), "Failed to get the URL for the settings page.");
    }

    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        Console.WriteLine("Received request for /api/test");
        return true;
    }

    public string GetUrlFromWebServer(string index)
    {
        _extensionWebServer = new WebServer.WebServer(_webContentPath);
        _extensionWebServer.RegisterRouteHandler("/api/test", HandleRequest);

        return $"http://localhost:{_extensionWebServer.Port}/{index}";
    }

    public string GetUrlFromFilePath(string index)
    {
        return Path.Combine(_webContentPath, index);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_extensionWebServer != null)
        {
            _extensionWebServer.Dispose();
        }
    }
}
