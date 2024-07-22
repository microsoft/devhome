// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace CoreWidgetProvider;

[ComVisible(true)]
#if CANARY_BUILD
[Guid("AED8A076-3C29-4783-8CFB-F629A5ADB748")]
#elif STABLE_BUILD
[Guid("426A52D6-8007-4894-A946-CF80F39507F1")]
#else
[Guid("1EAF0D53-0628-47F3-9C60-C70943FCA7CE")]
#endif
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
