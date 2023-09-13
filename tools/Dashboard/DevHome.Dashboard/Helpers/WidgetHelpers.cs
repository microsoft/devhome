// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Helpers;
internal class WidgetHelpers
{
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

    public static bool IsIncludedWidgetProvider(WidgetProviderDefinition provider)
    {
        var include = provider.Id.StartsWith("Microsoft.Windows.DevHome", StringComparison.CurrentCulture);
        Log.Logger()?.ReportInfo("WidgetHelpers", $"Found provider Id = {provider.Id}, include = {include}");
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

    public static async Task SetPositionCustomStateAsync(Widget widget, int ordinal)
    {
        var stateStr = await widget.GetCustomStateAsync();
        var state = JsonSerializer.Deserialize(stateStr, SourceGenerationContext.Default.WidgetCustomState);
        state.Position = ordinal;
        stateStr = JsonSerializer.Serialize(state, SourceGenerationContext.Default.WidgetCustomState);
        await widget.SetCustomStateAsync(stateStr);
    }
}
