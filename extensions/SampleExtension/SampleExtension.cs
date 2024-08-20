// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
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
            default:
                return null;
        }
    }

    public void Dispose()
    {
        _extensionDisposedEvent.Set();
    }
}
