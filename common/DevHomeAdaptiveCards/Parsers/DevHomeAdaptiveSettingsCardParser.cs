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

public class DevHomeAdaptiveSettingsCardParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var adaptiveSettingsCard = new DevHomeAdaptiveSettingsCard();

        adaptiveSettingsCard.Id = inputJson.GetNamedString("id");
        adaptiveSettingsCard.Description = inputJson.GetNamedString("Description");
        adaptiveSettingsCard.Header = inputJson.GetNamedString("Header");
        adaptiveSettingsCard.HeaderIcon = inputJson.GetNamedString("HeaderIcon");
        adaptiveSettingsCard.ShouldShowActionItem = inputJson.GetNamedBoolean("ShouldShowActionItem");

        if (inputJson.ContainsKey("id"))
        {
            adaptiveSettingsCard.Id = inputJson.GetNamedString("id");
        }

        if (inputJson.ContainsKey("Description"))
        {
            adaptiveSettingsCard.Description = inputJson.GetNamedString("Description");
        }

        if (inputJson.ContainsKey("Header"))
        {
            adaptiveSettingsCard.Header = inputJson.GetNamedString("Header");
        }

        if (inputJson.ContainsKey("HeaderIcon"))
        {
            adaptiveSettingsCard.HeaderIcon = inputJson.GetNamedString("HeaderIcon");
        }

        if (inputJson.ContainsKey("ShouldShowActionItem"))
        {
            adaptiveSettingsCard.ShouldShowActionItem = inputJson.GetNamedBoolean("ShouldShowActionItem");
        }

        if (inputJson.ContainsKey("ActionElement"))
        {
            var actionElementJson = inputJson.GetNamedObject("ActionElement");
            adaptiveSettingsCard.ActionElement = (IDevHomeAdaptiveSettingsCardAction)actionParsers.Get("ActionElement").FromJson(actionElementJson, elementParsers, actionParsers, warnings);
        }

        return adaptiveSettingsCard;
    }
}