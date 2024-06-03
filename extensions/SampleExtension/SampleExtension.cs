// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Windows.DevHome.SDK;

namespace SampleExtension;

[ComVisible(true)]
[Guid("BEA53870-57BA-4741-B849-DBC8A3A06CC6")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class SampleExtension : IExtension
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    public SampleExtension(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.DeveloperId:
                return new DeveloperIdProvider();
            case ProviderType.Repository:
                return new RepositoryProvider();
            case ProviderType.FeaturedApplications:
                return new FeaturedApplicationsProvider();
            default:
                return null;
        }
    }

    public void Dispose()
    {
        this._extensionDisposedEvent.Set();
    }
}
