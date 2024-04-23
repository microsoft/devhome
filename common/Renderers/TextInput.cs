// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Renderers;

public class TextInput : IAdaptiveElementRenderer
{
    public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AdaptiveTextInputRenderer();
        var elementToReturn = renderer.Render(element, context, renderArgs);

        if (element as AdaptiveTextInput is AdaptiveTextInput textInputElement)
        {
            if (textInputElement.InlineAction == null)
            {
                return elementToReturn;
            }

            if (textInputElement.InlineAction is not ChooseFileAction)
            {
                return elementToReturn;
            }

            // If the Input has an inline action, the element will have a button as a descendant.
            // Since guidance suggests inline actions use an icon rather than text, we can safely
            // set the content of the button to an icon.
            foreach (var descendant in elementToReturn.FindDescendants())
            {
                if (descendant is Button inlineActionButton)
                {
                    inlineActionButton.Padding = new Thickness(5);
                    inlineActionButton.Content = new FontIcon
                    {
                        Glyph = "\xED25",
                    };
                }
            }
        }

        return elementToReturn;
    }
}
