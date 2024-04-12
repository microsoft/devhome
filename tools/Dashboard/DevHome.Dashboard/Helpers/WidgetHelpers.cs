// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Helpers;

internal sealed class WidgetHelpers
{
    public const string WebExperiencePackPackageId = "9MSSGKG348SP";
    public const string WebExperiencePackageFamilyName = "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy";
    public const string WidgetServiceStorePackageId = "9N3RK8ZV2ZR8";
    public const string WidgetServicePackageFamilyName = "Microsoft.WidgetsPlatformRuntime_8wekyb3d8bbwe";

    public static readonly string[] DefaultWidgetDefinitionIds =
    {
    #if CANARY_BUILD
        "Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_CPUUsage",
        "Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_GPUUsage",
        "Microsoft.Windows.DevHome.Canary_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_NetworkUsage",
    #elif STABLE_BUILD
        "Microsoft.Windows.DevHome_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_CPUUsage",
        "Microsoft.Windows.DevHome_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_GPUUsage",
        "Microsoft.Windows.DevHome_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_NetworkUsage",
    #else
        "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_CPUUsage",
        "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_GPUUsage",
        "Microsoft.Windows.DevHome.Dev_8wekyb3d8bbwe!App!!CoreWidgetProvider!!System_NetworkUsage",
    #endif
    };

    public const string DevHomeHostName = "DevHome";

    private const double WidgetPxHeightSmall = 146;
    private const double WidgetPxHeightMedium = 304;
    private const double WidgetPxHeightLarge = 462;

    public static WidgetSize GetLargestCapabilitySize(WidgetCapability[] capabilities)
    {
        // Guaranteed to have at least one capability
        var largest = capabilities[0].Size;

        foreach (var cap in capabilities)
        {
            if (cap.Size > largest)
            {
                largest = cap.Size;
            }
        }

        return largest;
    }

    public static WidgetSize GetDefaultWidgetSize(WidgetCapability[] capabilities)
    {
        // The default size of the widget should be prioritized as Medium, Large, Small.
        // This matches the size preferences of the Windows Widget Dashboard.
        if (capabilities.Any(cap => cap.Size == WidgetSize.Medium))
        {
            return WidgetSize.Medium;
        }
        else if (capabilities.Any(cap => cap.Size == WidgetSize.Large))
        {
            return WidgetSize.Large;
        }
        else if (capabilities.Any(cap => cap.Size == WidgetSize.Small))
        {
            return WidgetSize.Small;
        }
        else
        {
            // Return something in case new sizes are added.
            return capabilities[0].Size;
        }
    }

    public static double GetPixelHeightFromWidgetSize(WidgetSize size)
    {
        return size switch
        {
            WidgetSize.Small => WidgetPxHeightSmall,
            WidgetSize.Medium => WidgetPxHeightMedium,
            WidgetSize.Large => WidgetPxHeightLarge,
            _ => 0,
        };
    }

    public static async Task<bool> IsIncludedWidgetProviderAsync(WidgetProviderDefinition provider)
    {
        // Cut WidgetProviderDefinition id down to just the package family name.
        var providerId = provider.Id;
        var endOfPfnIndex = providerId.IndexOf('!', StringComparison.Ordinal);
        var familyNamePartOfProviderId = providerId[..endOfPfnIndex];

        // Get the list of packages that contain Dev Home widgets.
        var extensionService = Application.Current.GetService<IExtensionService>();
        var enabledWidgetProviderIds = await extensionService.GetInstalledDevHomeWidgetPackageFamilyNamesAsync(true);

        // Check if the specified widget provider is in the list.
        var include = enabledWidgetProviderIds.ToList().Contains(familyNamePartOfProviderId);
        var log = Log.ForContext("SourceContext", nameof(WidgetHelpers));
        log.Information($"Found provider Id = {providerId}, include = {include}");
        return include;
    }

    public static string CreateWidgetCustomState(int ordinal)
    {
        var state = new WidgetCustomState
        {
            Host = DevHomeHostName,
            Position = ordinal,
        };

        return JsonSerializer.Serialize(state, SourceGenerationContext.Default.WidgetCustomState);
    }

    public static async Task SetPositionCustomStateAsync(ComSafeWidget widget, int ordinal)
    {
        var stateStr = await widget.GetCustomStateAsync();
        var state = JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);
        state.Position = ordinal;
        stateStr = JsonSerializer.Serialize(state, SourceGenerationContext.Default.WidgetCustomState);
        await widget.SetCustomStateAsync(stateStr);
    }
}
