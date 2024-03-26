// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace CoreWidgetProvider;

[ComVisible(true)]
[Guid("426A52D6-8007-4894-A946-CF80F39507F1")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class CoreExtension : IExtension
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    public CoreExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.DeveloperId:
                return new object();
            case ProviderType.Repository:
                return new object();
            case ProviderType.FeaturedApplications:
                return new object();
            default:
                Log.Information("Invalid provider");
                return null;
        }
    }

    public void Dispose()
    {
        _extensionDisposedEvent.Set();
    }
}
