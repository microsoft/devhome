// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Runtime.InteropServices;
using CoreWidgetProvider.Helpers;
using Microsoft.Windows.DevHome.SDK;

namespace CoreWidgetProvider;

[ComVisible(true)]
[Guid("426A52D6-8007-4894-A946-CF80F39507F1")]
[ComDefaultInterface(typeof(IExtension))]
public sealed class CorePlugin : IExtension
{
    private readonly ManualResetEvent _pluginDisposedEvent;

    public CorePlugin(ManualResetEvent pluginDisposedEvent)
    {
        _pluginDisposedEvent = pluginDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        switch (providerType)
        {
            case ProviderType.DeveloperId:
                return new object();
            case ProviderType.Repository:
                return new object();
            case ProviderType.Settings:
                return new object();
            case ProviderType.FeaturedApplications:
                return new object();
            default:
                Log.Logger()?.ReportInfo("Invalid provider");
                return null;
        }
    }

    public void Dispose()
    {
        _pluginDisposedEvent.Set();
    }
}
