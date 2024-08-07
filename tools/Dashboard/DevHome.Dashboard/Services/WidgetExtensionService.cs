// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using DevHome.Common.Services;

namespace DevHome.Dashboard.Services;

internal sealed class WidgetExtensionService : IWidgetExtensionService
{
    private const string ExtensionUniqueIdStable = "Microsoft.Windows.DevHome_8wekyb3d8bbwe!App!PG-SP-ID1";
    private const string ExtensionUniqueIdCanary = "Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe!App!PG-SP-ID1";
    private const string ExtensionUniqueIdDev = "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App!PG-SP-ID1";

    private const string ProviderDefinitionStable = "Microsoft.Windows.DevHome_8wekyb3d8bbwe!App!!CoreWidgetProvider";
    private const string ProviderDefinitionCanary = "Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe!App!!CoreWidgetProvider";
    private const string ProviderDefinitionDev = "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App!!CoreWidgetProvider";

    private readonly IExtensionService _extensionService;

    public WidgetExtensionService(IExtensionService extensionService)
    {
        _extensionService = extensionService;
    }

    /// <inheritdoc/>
    public bool IsCoreWidgetProvider(string providerDefinitionId)
    {
        return providerDefinitionId.Equals(ProviderDefinitionStable, StringComparison.Ordinal) ||
               providerDefinitionId.Equals(ProviderDefinitionCanary, StringComparison.Ordinal) ||
               providerDefinitionId.Equals(ProviderDefinitionDev, StringComparison.Ordinal);
    }

    public async Task EnsureCoreWidgetExtensionStarted(string providerDefinitionId)
    {
        if (providerDefinitionId.StartsWith(ProviderDefinitionStable, StringComparison.Ordinal))
        {
            await EnsureExtensionStarted(ExtensionUniqueIdStable);
        }
        else if (providerDefinitionId.StartsWith(ProviderDefinitionCanary, StringComparison.Ordinal))
        {
            await EnsureExtensionStarted(ExtensionUniqueIdCanary);
        }
        else if (providerDefinitionId.StartsWith(ProviderDefinitionDev, StringComparison.Ordinal))
        {
            await EnsureExtensionStarted(ExtensionUniqueIdDev);
        }
    }

    private async Task EnsureExtensionStarted(string extensionUniqueId)
    {
        var extensionWrapper = _extensionService.GetInstalledExtension(extensionUniqueId);
        if (!extensionWrapper.IsRunning())
        {
            await extensionWrapper.StartExtensionAsync();
        }
    }
}
