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
    private readonly Uri _uri;
    private readonly string _webContentPath = Path.Combine(AppContext.BaseDirectory, "WebContent");
    private WebServer.WebServer? _extensionWebServer;

    string ISettingsProvider.DisplayName => Resources.GetResource(@"SettingsProviderDisplayName");

    public DisplayType DisplayType => DisplayType.WebView2;

    public SettingsProvider2()
    {
        // select a method to get the URL
        // _uri = GetUrlFromFilePath("ExtensionSettingsMainPage.html");

        // use the web server to serve the page
        _uri = GetUrlFromWebServer("ExtensionSettingsMainPage.html");

        // use a public website
        // _uri = new Uri("https://github.com/login");
    }

    public AdaptiveCardSessionResult GetSettingsAdaptiveCardSession()
    {
        _log.Information($"GetSettingsAdaptiveCardSession");

        return new AdaptiveCardSessionResult(new SettingsUIController());
    }

    public WebViewResult GetWebView()
    {
        if (_uri == null || string.IsNullOrEmpty(_uri.ToString()))
        {
            return new WebViewResult(new NotImplementedException(), "Failed to get the URL for the settings page.");
        }

        return new WebViewResult(_uri);
    }

    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        Console.WriteLine("Received request for /api/test");
        return true;
    }

    public Uri GetUrlFromWebServer(string index)
    {
        _extensionWebServer = new WebServer.WebServer(_webContentPath);
        _extensionWebServer.RegisterRouteHandler("/api/test", HandleRequest);

        return new Uri($"http://localhost:{_extensionWebServer.Port}/{index}");
    }

    public Uri GetUrlFromFilePath(string index)
    {
        return new Uri(Path.Combine(_webContentPath, index));
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
