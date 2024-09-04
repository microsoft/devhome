// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Helpers;
using SampleExtension.Providers;
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

    private readonly WebServer.WebServer _webServer;
    private readonly string _url = string.Empty;

    public SampleExtension(ManualResetEvent extensionDisposedEvent, IHost host)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
        _host = host;

        var webcontentPath = Path.Combine(AppContext.BaseDirectory, "WebContent");
        Console.WriteLine($"Web content path: {webcontentPath}");
        _webServer = new WebServer.WebServer(webcontentPath);
        _webServer.RegisterRouteHandler("/api/test", HandleRequest);

        Console.WriteLine($"GitHubExtension is running on port {_webServer.Port}");

        // using web server:
        string extensionSettingsWebPage = "ExtensionSettingsPage.html";
        _url = $"http://localhost:{_webServer.Port}/{extensionSettingsWebPage}";
        Console.WriteLine($"Navigate to: {_url}");

        // using file path:
        string filePath = Path.Combine(webcontentPath, "HelloWorld.html");
        Console.WriteLine($"filePath: {filePath}");

        // _url = filePath;
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
        _webServer.Dispose();
    }

    public bool HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        Console.WriteLine("Received request for /api/test");
        return true;
    }
}
