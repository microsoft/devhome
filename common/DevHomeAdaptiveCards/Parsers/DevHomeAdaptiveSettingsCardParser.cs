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

        if (inputJson.ContainsKey("ActionElement"))
        {
            var actionElementJson = inputJson.GetNamedObject("ActionElement");
            var actionElementType = actionElementJson.GetNamedString("type");

            if (string.Equals(actionElementType, DevHomeAdaptiveSettingsCardLaunchContentDialogButton.AdaptiveSettingsCardActionType, StringComparison.OrdinalIgnoreCase))
            {
                adaptiveSettingsCard.ActionElement = CreateLaunchContentDialogButton(actionElementJson, elementParsers, actionParsers, warnings);
            }
        }

        return adaptiveSettingsCard;
    }

    private DevHomeAdaptiveSettingsCardLaunchContentDialogButton CreateLaunchContentDialogButton(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var parser = actionParsers.Get(DevHomeAdaptiveSettingsCardLaunchContentDialogButton.AdaptiveSettingsCardActionType);
        return (DevHomeAdaptiveSettingsCardLaunchContentDialogButton)parser.FromJson(inputJson, elementParsers, actionParsers, warnings);
    }
}
