// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.AdaptiveCardInputValues;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Renderers;

/// <summary>
/// Renders a list of expanded choice set items as an ItemsView
/// </summary>
public class ItemsViewChoiceSet : IAdaptiveElementRenderer
{
    /// <summary>
    /// The DataTemplate to be used for the ItemsView. This will be used to allow consumers to specify how the items
    /// look within the ItemsView.
    /// </summary>
    private readonly DataTemplate _itemsTemplate;

    /// <summary>
    /// The items to be displayed in the ItemsView. The items are objects which will be used as the Content
    /// of the ListViewItems we add to the ItemsView. The datatemplate should use Binding to display values from
    /// these objects.
    /// </summary>
    /// <remarks>
    /// The order of the items in this list will should be the same order as the items in the choice set.
    /// </remarks>
    private readonly List<object> _originalItems;

    public ItemsView ChoiceSetItemsView { get; private set; } = new();

    public List<ListViewItem> ListViewItemsForItemsView { get; private set; } = new();

    public ItemsViewChoiceSet(DataTemplate itemsTemplate, List<object> objectList)
    {
        _itemsTemplate = itemsTemplate;
        _originalItems = objectList;

        // set the template for the items view.
        ChoiceSetItemsView.ItemTemplate = _itemsTemplate;
    }

    public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var choiceSet = element as AdaptiveChoiceSetInput;

        // We'll only allow this renderer to be used if the choice set is expanded.
        // If it's not expanded, we'll just use the default renderer. (Note: expanded is visually a list of radio buttons)
        // we'' use the custom renderer to show these as a list of items, instead of radio buttons.
        if (choiceSet != null && choiceSet.ChoiceSetStyle == ChoiceSetStyle.Expanded)
        {
            if (choiceSet.Choices.Count != ListViewItemsForItemsView.Count)
            {
                throw new ArgumentException("The number of items in the choice set must match the number of items in the ItemsViewChoiceSet.");
            }

            // Check if the choice set is multi-select, and if it is make sure the ItemsView is set to allow multiple selection.
            if (choiceSet.IsMultiSelect)
            {
                ChoiceSetItemsView.SelectionMode = ItemsViewSelectionMode.Multiple;
            }

            // Go through all the items in the choice set and make an item for each one.
            for (var i = 0; i < choiceSet.Choices.Count; i++)
            {
                choiceSet.AdditionalProperties.TryGetValue("value", out var value);
                ListViewItemsForItemsView.Add(new ListViewItem() { Content = _originalItems[i] });
            }

            // Set upp the ItemsSource for the ItemsView and add the input value to the context.
            // the input value is used to get the current index of the items view in relation
            // to the item in the choice set.
            ChoiceSetItemsView.ItemsSource = ListViewItemsForItemsView;
            context.AddInputValue(new ItemsViewInputValue(choiceSet, ChoiceSetItemsView), renderArgs);

            // Return the ItemsView.
            return ChoiceSetItemsView;
        }

        if (element is AdaptiveActionSet actionSet)
        {
            actionSet.Actions.ForEach(action =>
            {
                if (action is AdaptiveExecuteAction executeAction)
                {
                    LinkSubmitActionToCard(executeAction, context, renderArgs);
                }
                else if (action is AdaptiveSubmitAction submitAction)
                {
                    LinkSubmitActionToCard(submitAction, context, renderArgs);
                }
            });
        }

        // Use default render for all other cases.
        var renderer = new AdaptiveChoiceSetInputRenderer();
        return renderer.Render(element, context, renderArgs);
    }
}
