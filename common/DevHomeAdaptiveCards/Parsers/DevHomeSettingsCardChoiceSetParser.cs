// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.Parsers;

public class DevHomeSettingsCardChoiceSetParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var adaptiveSettingsCardChoiceSet = new DevHomeSettingsCardChoiceSet();

        if (inputJson.ContainsKey("id"))
        {
            adaptiveSettingsCardChoiceSet.Id = inputJson.GetNamedString("id");
        }

        if (inputJson.ContainsKey("DevHomeContentGroupDescription"))
        {
            adaptiveSettingsCardChoiceSet.GroupDescription = inputJson.GetNamedString("GroupDescription");
        }

        if (inputJson.ContainsKey("SelectedValue"))
        {
            adaptiveSettingsCardChoiceSet.SelectedValue = (int)inputJson.GetNamedNumber("SelectedValue");
        }

        if (inputJson.ContainsKey("IsMultiSelect"))
        {
            adaptiveSettingsCardChoiceSet.IsMultiSelect = inputJson.GetNamedBoolean("IsMultiSelect");
        }

        // If IsSelectionDisabled is true, then IsMultiSelect should be false and no item should be selected.
        if (inputJson.ContainsKey("DevHomeSettingsCardIsSelectionDisabled"))
        {
            adaptiveSettingsCardChoiceSet.IsSelectionDisabled = inputJson.GetNamedBoolean("IsMultiSelect");

            if (adaptiveSettingsCardChoiceSet.IsSelectionDisabled)
            {
                adaptiveSettingsCardChoiceSet.SelectedValue = DevHomeSettingsCardChoiceSet.UnselectedIndex;
                adaptiveSettingsCardChoiceSet.IsMultiSelect = false;
            }
        }

        // Parse the settings cards
        if (inputJson.ContainsKey("DevHomeSettingsCards"))
        {
            var elementJson = inputJson.GetNamedArray("DevHomeSettingsCards");
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
            if (parser.FromJson(element.GetObject(), elementParsers, actionParsers, warnings) is DevHomeSettingsCard card)
            {
                settingsCards.Add(card);
            }
        }

        return settingsCards;
    }
}
