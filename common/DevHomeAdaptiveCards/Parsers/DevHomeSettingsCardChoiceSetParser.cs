// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Reflection.Emit;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Environments.Helpers;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.Parsers;

public class DevHomeSettingsCardChoiceSetParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var adaptiveSettingsCardChoiceSet = new DevHomeSettingsCardChoiceSet();
        bool isCorrectType;

        if (inputJson.TryGetValue("id", out var id))
        {
            isCorrectType = id.ValueType == JsonValueType.String;
            adaptiveSettingsCardChoiceSet.Id = isCorrectType ? id.GetString() : string.Empty;
        }

        if (inputJson.TryGetValue("label", out var label))
        {
            isCorrectType = label.ValueType == JsonValueType.String;
            adaptiveSettingsCardChoiceSet.Label = isCorrectType ? label.GetString() : StringResourceHelper.GetResource("SettingsCardChoiceSetDefaultLabel");
        }

        if (inputJson.TryGetValue("SelectedValue", out var selectedValue))
        {
            isCorrectType = selectedValue.ValueType == JsonValueType.Number;
            adaptiveSettingsCardChoiceSet.SelectedValue = isCorrectType ? (int)selectedValue.GetNumber() : DevHomeSettingsCardChoiceSet.UnselectedIndex;
        }

        if (inputJson.TryGetValue("IsMultiSelect", out var isMultiSelect))
        {
            isCorrectType = isMultiSelect.ValueType == JsonValueType.Boolean;
            adaptiveSettingsCardChoiceSet.IsMultiSelect = isCorrectType ? isMultiSelect.GetBoolean() : false;
        }

        // If IsSelectionDisabled is true, then IsMultiSelect should be false and no item should be selected.
        if (inputJson.TryGetValue("DevHomeSettingsCardIsSelectionDisabled", out var devHomeSettingsCardIsSelectionDisabled))
        {
            isCorrectType = devHomeSettingsCardIsSelectionDisabled.ValueType == JsonValueType.Boolean;
            adaptiveSettingsCardChoiceSet.IsSelectionDisabled = isCorrectType ? devHomeSettingsCardIsSelectionDisabled.GetBoolean() : false;

            if (adaptiveSettingsCardChoiceSet.IsSelectionDisabled)
            {
                adaptiveSettingsCardChoiceSet.SelectedValue = DevHomeSettingsCardChoiceSet.UnselectedIndex;
                adaptiveSettingsCardChoiceSet.IsMultiSelect = false;
            }
        }

        // Parse the settings cards
        if (inputJson.TryGetValue("DevHomeSettingsCards", out var devHomeSettingsCards))
        {
            isCorrectType = devHomeSettingsCards.ValueType == JsonValueType.Array;
            var elementJson = isCorrectType ? devHomeSettingsCards.GetArray() : [];
            adaptiveSettingsCardChoiceSet.SettingsCards = GetSettingsCards(elementJson, elementParsers, actionParsers, warnings);
        }

        return adaptiveSettingsCardChoiceSet;
    }

    private List<DevHomeSettingsCard> GetSettingsCards(JsonArray elementJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        List<DevHomeSettingsCard> settingsCards = new();
        var parser = elementParsers.Get(DevHomeSettingsCard.AdaptiveElementType);
        foreach (var element in elementJson)
        {
            if (element.ValueType != JsonValueType.Object)
            {
                continue;
            }

            if (parser.FromJson(element.GetObject(), elementParsers, actionParsers, warnings) is DevHomeSettingsCard card)
            {
                settingsCards.Add(card);
            }
        }

        return settingsCards;
    }
}
