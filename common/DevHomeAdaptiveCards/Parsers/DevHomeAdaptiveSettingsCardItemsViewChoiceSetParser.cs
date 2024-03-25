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

public class DevHomeAdaptiveSettingsCardItemsViewChoiceSetParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var adaptiveSettingsCardChoiceSet = new DevHomeAdaptiveSettingsCardItemsViewChoiceSet();

        if (inputJson.ContainsKey("id"))
        {
            adaptiveSettingsCardChoiceSet.Id = inputJson.GetNamedString("id");
        }

        if (inputJson.ContainsKey("GroupDescription"))
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

        if (inputJson.ContainsKey("SettingsCards"))
        {
            var elementJson = inputJson.GetNamedArray("SettingsCards");
            adaptiveSettingsCardChoiceSet.SettingsCards = GetSettingsCards(elementJson, elementParsers, actionParsers, warnings);
        }

        return adaptiveSettingsCardChoiceSet;
    }

    private List<DevHomeAdaptiveSettingsCard> GetSettingsCards(JsonArray elementJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        List<DevHomeAdaptiveSettingsCard> settingsCards = new();
        var parser = elementParsers.Get(DevHomeAdaptiveSettingsCard.AdaptiveSettingsCardType);
        foreach (var element in elementJson)
        {
            settingsCards.Add((DevHomeAdaptiveSettingsCard)parser.FromJson(element.GetObject(), elementParsers, actionParsers, warnings));
        }

        return settingsCards;
    }
}
