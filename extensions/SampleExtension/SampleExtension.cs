// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.SettingsProvider;
using Serilog;

namespace SampleExtension;

[ComVisible(true)]
#if CANARY_BUILD
[Guid("EDAF64A1-B163-4E7E-935D-679C950704FE")]
#elif STABLE_BUILD
[Guid("83650A38-FFE6-4F84-BF36-92674EB39738")]
#else
[Guid("BEA53870-57BA-4741-B849-DBC8A3A06CC6")]
#endif
[ComDefaultInterface(typeof(IExtension))]
public sealed class SampleExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly IHost _host;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SampleExtension));

    private readonly string _url;
    private readonly string _webContentPath = Path.Combine(AppContext.BaseDirectory, "WebContent");
    private WebServer.WebServer? _extensionWebServer;

    public SampleExtension(ManualResetEvent extensionDisposedEvent, IHost host)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
        _host = host;

        // select a method to get the URL
        // _url = GetUrlFromFilePath("ExtensionSettingsPage.html");

        // use the web server to serve the page
        _url = GetUrlFromWebServer("ExtensionSettingsPage.html");

        // use a public website
        // _url = "https://github.com/login";
    }

    public object? GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.DeveloperId:
                return new DeveloperIdProvider();
            case ProviderType.Repository:
                return new RepositoryProvider();
            case ProviderType.FeaturedApplications:
                return new FeaturedApplicationsProvider();
            case ProviderType.Settings:
                return new SettingsProvider2(new WebViewResult(_url));
            default:
                return null;
        }
    }

    public void Dispose()
    {
        _extensionDisposedEvent.Set();
        if (_extensionWebServer != null)
        {
            _extensionWebServer.Dispose();
        }
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
}
