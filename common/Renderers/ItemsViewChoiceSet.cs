// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.WinUI.Controls;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.DevHomeAdaptiveCards.InputValues;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace DevHome.Common.Renderers;

public enum DevHomeChoiceSetKind
{
    Unknown,
    ItemsViewChoiceSet,
}

/// <summary>
/// Renders a known Dev Home choice set as an ItemsView.
/// </summary>
public class ItemsViewChoiceSet : IAdaptiveElementRenderer
{
    private readonly double _defaultSpacing = 5;

    private readonly StackLayout _defaultLayout = new();

    public ItemsView ChoiceSetItemsView { get; private set; } = new();

    public List<ItemContainer> ItemsContainerList { get; private set; } = new();

    public ItemsViewChoiceSet(string itemsTemplateResourceName)
    {
        // set the template for the items view.
        var itemsTemplate = Application.Current.Resources[itemsTemplateResourceName] as DataTemplate;
        ChoiceSetItemsView.ItemTemplate = itemsTemplate;
        _defaultLayout.Spacing = _defaultSpacing;
        ChoiceSetItemsView.Layout = _defaultLayout;
    }

    // Default template for the ItemsView will be used
    public ItemsViewChoiceSet()
    {
        _defaultLayout.Spacing = _defaultSpacing;
        ChoiceSetItemsView.Layout = _defaultLayout;
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
        // Check if the choice set is multi-select, and if it is make sure the ItemsView is set to allow multiple selection.
        if (settingsCardChoiceSet.IsMultiSelect)
        {
            ChoiceSetItemsView.SelectionMode = ItemsViewSelectionMode.Multiple;
        }

        // If selection is disabled, set the ItemsView to not allow selection of items in the items view.
        if (settingsCardChoiceSet.IsSelectionDisabled)
        {
            ChoiceSetItemsView.SelectionMode = ItemsViewSelectionMode.None;
        }

        // Go through all the items in the choice set and make an item for each one.
        for (var i = 0; i < settingsCardChoiceSet.SettingsCards.Count; i++)
        {
            var curCard = settingsCardChoiceSet.SettingsCards[i];
            curCard.HeaderIconImage = AdaptiveCardHelpers.ConvertBase64StringToImageSource(curCard.HeaderIcon);
        }

        // Set up the ItemsSource for the ItemsView and add the input value to the context.
        // the input value is used to get the current index of the items view in relation
        // to the item in the choice set.
        ChoiceSetItemsView.ItemsSource = settingsCardChoiceSet.SettingsCards;

        // Set the automation name of the list to be the label of the choice set.
        context.AddInputValue(new ItemsViewInputValue(settingsCardChoiceSet, ChoiceSetItemsView), renderArgs);
        AutomationProperties.SetName(ChoiceSetItemsView, settingsCardChoiceSet.Label);

        // Return the ItemsView.
        return ChoiceSetItemsView;
    }
}
