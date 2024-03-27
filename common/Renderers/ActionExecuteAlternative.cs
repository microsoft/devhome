// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Common.Renderers;

public class ActionExecuteAlternative : IAdaptiveActionRenderer
{
    public UIElement? Render(IAdaptiveActionElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AdaptiveExecuteActionRenderer();

        if (element is AdaptiveExecuteAction)
        {
            var result = renderer.Render(element, context, renderArgs) as Button;
            if (result != null)
            {
                result.Background = new SolidColorBrush(Colors.DarkSlateGray);
                result.Foreground = new SolidColorBrush(Colors.White);
                result.BorderBrush = new SolidColorBrush(Colors.DarkSlateGray);
                result.BorderThickness = new Thickness(1);
                result.Margin = new Thickness(0, 0, 0, 8);
                return result;
            }
        }

        // For other types of actions, you might need to handle them differently or return null
        return renderer.Render(element, context, renderArgs);
    }
}
