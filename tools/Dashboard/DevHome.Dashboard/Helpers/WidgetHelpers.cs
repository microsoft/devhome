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

    public static WidgetSize GetDefaultWidgetSize(WidgetCapability[] capabilities)
    {
        // The default size of the widget should be priortized as Medium, Large, Small.
        // This matches the size preferences of the Windows Widget Dashboard.
        var sizeS = false;
        var sizeM = false;
        var sizeL = false;

        foreach (var cap in capabilities)
        {
            switch (cap.Size)
            {
                case WidgetSize.Small:
                    sizeS = true; break;
                case WidgetSize.Medium:
                    sizeM = true; break;
                case WidgetSize.Large:
                    sizeL = true; break;
            }
        }

        if (sizeM)
        {
            return WidgetSize.Medium;
        }
        else if (sizeL)
        {
            return WidgetSize.Large;
        }
        else if (sizeS)
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
}
