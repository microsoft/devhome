// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.DevHomeAdaptiveCards.InputValues;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Renderers;

/// <summary>
/// Renders a known Dev Home choice set as an ItemsView.
/// </summary>
public class ItemsViewChoiceSet : IAdaptiveElementRenderer
{
    private readonly double _defaultSpacing = 5;

    private readonly string? _choiceSetItemsTemplateName;

    public ItemsViewChoiceSet(string itemsTemplateResourceName)
    {
        // set the template for the items view.
        _choiceSetItemsTemplateName = itemsTemplateResourceName;
    }

    // Default template for the ItemsView will be used
    public ItemsViewChoiceSet()
    {
    }

    public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        // As we add more types of choice sets, we can add more cases here.
        if (element is DevHomeSettingsCardChoiceSet settingsCardChoiceSet)
        {
            return GetItemsViewElement(settingsCardChoiceSet, context, renderArgs);
        }

        // Use default render for all other cases.
        var renderer = new AdaptiveChoiceSetInputRenderer();
        return renderer.Render(element, context, renderArgs);
    }

    private ItemsView GetItemsViewElement(DevHomeSettingsCardChoiceSet settingsCardChoiceSet, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        // Set default spacing for the ItemsView.
        var choiceSetItemsView = new ItemsView();
        var defaultLayout = new StackLayout();
        defaultLayout.Spacing = _defaultSpacing;
        choiceSetItemsView.Layout = defaultLayout;

        // If there is a template for the items view, set it.
        if (!string.IsNullOrEmpty(_choiceSetItemsTemplateName))
        {
            choiceSetItemsView.ItemTemplate = Application.Current.Resources[_choiceSetItemsTemplateName] as DataTemplate;
        }

        // Check if the choice set is multi-select, and if it is make sure the ItemsView is set to allow multiple selection.
        if (settingsCardChoiceSet.IsMultiSelect)
        {
            choiceSetItemsView.SelectionMode = ItemsViewSelectionMode.Multiple;
        }

        // If selection is disabled, set the ItemsView to not allow selection of items in the items view.
        if (settingsCardChoiceSet.IsSelectionDisabled)
        {
            choiceSetItemsView.SelectionMode = ItemsViewSelectionMode.None;
        }

        // Go through all the items in the choice set and make an item for each one.
        for (var i = 0; i < settingsCardChoiceSet.SettingsCards.Count; i++)
        {
            var curCard = settingsCardChoiceSet.SettingsCards[i];
            curCard.HeaderIconImage = AdaptiveCardHelpers.ConvertBase64StringToImageIcon(curCard.HeaderIcon);
        }

        // Set up the ItemsSource for the ItemsView and add the input value to the context.
        // the input value is used to get the current index of the items view in relation
        // to the item in the choice set.
        choiceSetItemsView.ItemsSource = settingsCardChoiceSet.SettingsCards;

        // Set the automation name of the list to be the label of the choice set.
        context.AddInputValue(new ItemsViewInputValue(settingsCardChoiceSet, choiceSetItemsView), renderArgs);
        AutomationProperties.SetName(choiceSetItemsView, settingsCardChoiceSet.Label);

        // Return the ItemsView.
        return choiceSetItemsView;
    }
}
