// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Helpers;
internal class WidgetHelpers
{
    private const double WidgetPxHeightSmall = 146;
    private const double WidgetPxHeightMedium = 304;
    private const double WidgetPxHeightLarge = 462;

    public static WidgetSize GetLargetstCapabilitySize(WidgetCapability[] capabilities)
    {
        // Guaranteed to have at least one capability
        WidgetSize largest = capabilities[0].Size;

        foreach (var cap in capabilities)
        {
            if (cap.Size > largest)
            {
            largest = cap.Size;
            }
        }

        return largest;
    }

    public static double GetPixelHeightFromWidgetSize(WidgetSize size)
    {
        if (size == WidgetSize.Small)
        {
            return WidgetPxHeightSmall;
        }
        else if (size == WidgetSize.Medium)
        {
            return WidgetPxHeightMedium;
        }
        else
        {
            return WidgetPxHeightLarge;
        }
    }
}
