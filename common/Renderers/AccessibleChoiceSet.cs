// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Renderers;

public partial class AccessibleChoiceSet : IAdaptiveElementRenderer
{
    public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AdaptiveChoiceSetInputRenderer();

        if (element is AdaptiveChoiceSetInput choiceSet)
        {
            // Label property corresponds to the Header dependency property on the ComboBox.
            var header = choiceSet.Label;
            var placeholderText = choiceSet.Placeholder;

            // If there is no Header, there will not be an accessible Name.
            // Use the Placeholder text as the accessible Name if possible.
            if (string.IsNullOrEmpty(header) && !string.IsNullOrEmpty(placeholderText))
            {
                var result = renderer.Render(choiceSet, context, renderArgs);
                if (result is StackPanel stackPanel)
                {
                    var comboBox = stackPanel.Children.First() as ComboBox;
                    if (comboBox != null)
                    {
                        AutomationProperties.SetName(comboBox, placeholderText);
                        return stackPanel;
                    }
                }
            }
        }

        return renderer.Render(element, context, renderArgs);
    }
}
