// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using SampleExtension.Providers;
using Serilog;

namespace SampleExtension;

[ComVisible(true)]
[Guid("BEA53870-57BA-4741-B849-DBC8A3A06CC6")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class SampleExtension : IExtension
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly IHost _host;
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SampleExtension));

    public SampleExtension(ManualResetEvent extensionDisposedEvent, IHost host)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
        _host = host;
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
                return _host.Services.GetService<SettingsProvider>();
            case ProviderType.Navigation:
                return _host.Services.GetService<NavigationProvider>();
            default:
                return null;
        }
    }

    public void Dispose()
    {
        _extensionDisposedEvent.Set();
    }
}
