// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.DevHomeAdaptiveCards.InputValues;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Windows.Data.Json;

namespace DevHome.Common.Renderers;

/// <summary>
/// Custom renderer that will allow an adaptive card combo box to also contain a subtitle under its title value.
/// The render also provides the ability to refresh the card when a selection changes within a combo box. This
/// is used to retrieve new data for other elements in the adaptive card based on that selection.
/// </summary>
/// <remarks>
/// Adaptive cards do not currently support dynamically updating one or more elements within a card
/// in response to the data in another element within the card changing.
/// See issue in the adaptive card repository: https://github.com/microsoft/AdaptiveCards/issues/8598
/// To work around this we attach a callback to a UI element representing a choice set. This callback will then
/// update the data of another choiceSet or element in the adaptive card based on the current selection of the originating
/// choiceSet. This allows extensions to create an adaptive card where the selection in one combo box dynamically
/// updates the data in another combo box.
///
/// We do this by allowing a choiceSet to be the "parent" which contains data for its own choiceSet and all data needed
/// for a "child" choiceSet. We use the additionalProperties of the Input.ChoiceSet to achieve this. When the parent
/// selection changes, we use the index of the parent to get the correct list of new choices for the child choiceSet.
/// </remarks>
public partial class DevHomeChoiceSetWithDynamicRefresh : IAdaptiveElementRenderer
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(DevHomeChoiceSetWithDynamicRefresh));

    private const int UnSelectedIndex = -1;

    private readonly Dictionary<string, UIElement> _choiceSetIdToUIElementMap = new();

    private readonly Dictionary<string, string> _choiceSetParentIdToChildIdMap = new();

    /// <summary>
    /// Mapping for the parent UIElement of the parent choiceSet to a list of lists where each inner list contains the choices
    /// for an item within the parent choiceSet. The inner list is used as the data for a child choiceSet when the selection
    /// in the parent UIElement changes.
    /// </summary>
    private readonly Dictionary<UIElement, List<List<DevHomeChoicesData>>> _childChoiceSetDataForOnParentSelectionChanged = new();

    public UIElement Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var renderer = new AccessibleChoiceSet();

        if ((element is AdaptiveChoiceSetInput choiceSet) && (choiceSet.ChoiceSetStyle == ChoiceSetStyle.Compact))
        {
            // Card author requesting multiline combo box with the second line being a subtitle for the comboBox item
            if ((GetDevHomeChoicesWithSubtitles(choiceSet) is List<DevHomeChoicesData> choices) && (choices.Count > 0))
            {
                return SetupComboBoxWithSubtitleTemplate(choices, choiceSet, context, renderArgs);
            }

            // Card author requesting that the adaptive card be refreshed when the combo box selection changes.
            if (ShouldRefreshChildChoiceSetOnSelectionChange(choiceSet))
            {
                return SetupParentComboBoxForDynamicRefresh(choiceSet, context, renderArgs);
            }
        }

        return renderer.Render(element, context, renderArgs);
    }

    private Grid SetupParentComboBoxForDynamicRefresh(AdaptiveChoiceSetInput choiceSet, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        var listOfComboBoxItems = new List<ComboBoxItem>();
        for (var i = 0; i < choiceSet.Choices.Count; i++)
        {
            var item = new ComboBoxItem();
            item.Content = choiceSet.Choices[i].Title;
            listOfComboBoxItems.Add(item);
        }

        var comboBox = new ComboBox();
        comboBox.ItemsSource = listOfComboBoxItems;
        comboBox.SelectedIndex = int.TryParse(choiceSet.Value, out var selectedIndex) ? selectedIndex : UnSelectedIndex;

        // Setup event handlers
        comboBox.SelectionChanged += RefreshCardOnSelectionChanged;
        comboBox.Unloaded += RemoveEventHandler;

        // Use the choiceSets Id as the name of the combo box.
        comboBox.Name = choiceSet.Id;
        comboBox.PlaceholderText = choiceSet.Placeholder;
        var gridForComboBoxWithRefresh = new Grid();
        gridForComboBoxWithRefresh.Children.Add(comboBox);

        // Link the input value to the renders context so the correct index/value is sent when the user submit the adaptive card.
        context.AddInputValue(new CustomComboBoxInputValue(choiceSet, comboBox), renderArgs);

        // Add the data that will be queried when the selection of the combo box changes. The selected index of the combo box will
        // provide the index of the new data to be used for the child choice set when the selection changes.
        AddChildChoiceSetDataForSelectionChanged(choiceSet, comboBox);
        LinkParentIdToChildId(choiceSet);
        AddChoiceSetIdToMap(choiceSet.Id, comboBox);
        return gridForComboBoxWithRefresh;
    }

    private Grid SetupComboBoxWithSubtitleTemplate(
        List<DevHomeChoicesData> choices,
        AdaptiveChoiceSetInput choiceSet,
        AdaptiveRenderContext context,
        AdaptiveRenderArgs renderArgs)
    {
        var gridForUiElement = new Grid();

        var comboBox = new ComboBox();
        comboBox.ItemsSource = choices;
        comboBox.ItemTemplate = Application.Current.Resources["ChoiceSetWithSubtitleTemplate"] as DataTemplate;
        comboBox.SelectedIndex = int.TryParse(choiceSet.Value, out var selectedIndex) ? selectedIndex : UnSelectedIndex;

        // Use the choiceSets Id as the name of the combo box.
        comboBox.Name = choiceSet.Id;
        comboBox.PlaceholderText = choiceSet.Placeholder;
        gridForUiElement.Children.Add(comboBox);

        // Link the input value to the renders context so the correct index/value is sent when the user submit the adaptive card.
        context.AddInputValue(new CustomComboBoxInputValue(choiceSet, comboBox), renderArgs);
        AddChoiceSetIdToMap(choiceSet.Id, comboBox);

        return gridForUiElement;
    }

    private void AddChoiceSetIdToMap(string id, UIElement uiElement)
    {
        if (!string.IsNullOrEmpty(id))
        {
            _choiceSetIdToUIElementMap[id] = uiElement;
        }
    }

    private void LinkParentIdToChildId(AdaptiveChoiceSetInput choiceSet)
    {
        var childId = GetChildChoiceSetId(choiceSet);
        if (!string.IsNullOrEmpty(childId))
        {
            _choiceSetParentIdToChildIdMap[choiceSet.Id] = childId;
        }
    }

    private List<DevHomeChoicesData> GetDevHomeChoicesWithSubtitles(AdaptiveChoiceSetInput choiceSet)
    {
        if (choiceSet.AdditionalProperties.TryGetValue("devHomeChoicesData", out var choices) && (choices.ValueType == JsonValueType.Array))
        {
            return JsonSerializer.Deserialize<List<DevHomeChoicesData>>(choices.GetArray().ToString()) ?? new List<DevHomeChoicesData>();
        }

        return new List<DevHomeChoicesData>();
    }

    private void AddChildChoiceSetDataForSelectionChanged(AdaptiveChoiceSetInput choiceSet, UIElement uiElement)
    {
        choiceSet.AdditionalProperties.TryGetValue("devHomeSelectionChangedDataForChildChoiceSet", out var selectionChangedData);
        if ((selectionChangedData == null) || (selectionChangedData.ValueType != JsonValueType.Array))
        {
            return;
        }

        // Build the list of "choice" data that will be updated when the parent choiceSet's selection changes.
        var listOfAllChoices = new List<List<DevHomeChoicesData>>();
        foreach (var choicesList in selectionChangedData.GetArray())
        {
            if (choicesList.ValueType != JsonValueType.Array)
            {
                continue;
            }

            var deserializedChoices = JsonSerializer.Deserialize<List<DevHomeChoicesData>>(choicesList.GetArray().ToString());
            if (deserializedChoices is List<DevHomeChoicesData>)
            {
                listOfAllChoices.Add(deserializedChoices);
            }
        }

        if (listOfAllChoices.Count > 0)
        {
            _childChoiceSetDataForOnParentSelectionChanged[uiElement] = listOfAllChoices;
        }
    }

    private bool ShouldRefreshChildChoiceSetOnSelectionChange(AdaptiveChoiceSetInput choiceSet)
    {
        if (choiceSet.AdditionalProperties.TryGetValue("devHomeRefreshChildChoiceSetOnSelectionChanged", out var shouldRefresh) && (shouldRefresh.ValueType == JsonValueType.Boolean))
        {
            return shouldRefresh.GetBoolean();
        }

        return false;
    }

    private string GetChildChoiceSetId(AdaptiveChoiceSetInput choiceSet)
    {
        if (choiceSet.AdditionalProperties.TryGetValue("devHomeChildChoiceSetId", out var childId) && (childId.ValueType == JsonValueType.String))
        {
            return childId.GetString();
        }

        return string.Empty;
    }

    /// <summary>
    /// When the parent choiceSet's selection changes refresh the child choiceSet with new data.
    /// </summary>
    private void RefreshCardOnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (sender is ComboBox parentComboBox && _choiceSetParentIdToChildIdMap.TryGetValue(parentComboBox.Name, out var choiceSetChildId))
        {
            _choiceSetIdToUIElementMap.TryGetValue(choiceSetChildId, out var choiceSetToUpdate);

            if (choiceSetToUpdate is ComboBox comboBoxToUpdate)
            {
                UpdateComboBoxWithDynamicData(parentComboBox, comboBoxToUpdate);
            }
        }
    }

    private void RemoveEventHandler(object sender, object args)
    {
        if (sender is ComboBox parentComboBox)
        {
            parentComboBox.SelectionChanged -= RefreshCardOnSelectionChanged;
            parentComboBox.Unloaded -= RemoveEventHandler;
            _childChoiceSetDataForOnParentSelectionChanged.Remove(parentComboBox);
            if (_choiceSetParentIdToChildIdMap.TryGetValue(parentComboBox.Name, out var childComboBoxId))
            {
                _choiceSetIdToUIElementMap.Remove(childComboBoxId);
            }
        }
    }

    private void UpdateComboBoxWithDynamicData(ComboBox parentComboBox, ComboBox childComboBox)
    {
        _childChoiceSetDataForOnParentSelectionChanged.TryGetValue(parentComboBox, out var newDataForChildComboBox);

        if (newDataForChildComboBox == null)
        {
            _log.Error($"Couldn't find the parent combo box with name {parentComboBox.Name} in _childChoiceSetDataForOnParentSelectionChanged map");
            return;
        }

        if ((parentComboBox.SelectedIndex < 0) || (parentComboBox.SelectedIndex > newDataForChildComboBox.Count))
        {
            _log.Error($"the selectedIndex in for {parentComboBox.Name}: '{parentComboBox.SelectedIndex}' is invalid for {nameof(newDataForChildComboBox)} list with count: {newDataForChildComboBox.Count}");
            return;
        }

        childComboBox.ItemsSource = newDataForChildComboBox[parentComboBox.SelectedIndex];
    }

    private void ResetMappings()
    {
        _choiceSetParentIdToChildIdMap.Clear();
        _choiceSetIdToUIElementMap.Clear();
        _childChoiceSetDataForOnParentSelectionChanged.Clear();
    }
}
