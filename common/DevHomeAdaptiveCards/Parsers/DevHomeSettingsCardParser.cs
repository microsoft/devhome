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

public class DevHomeSettingsCardParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var adaptiveSettingsCard = new DevHomeSettingsCard();

        if (inputJson.ContainsKey("id"))
        {
            adaptiveSettingsCard.Id = inputJson.GetNamedString("id");
        }

        if (inputJson.ContainsKey("DevHomeSettingsCardDescription"))
        {
            adaptiveSettingsCard.Description = inputJson.GetNamedString("DevHomeSettingsCardDescription");
        }

        if (inputJson.ContainsKey("DevHomeSettingsCardHeader"))
        {
            adaptiveSettingsCard.Header = inputJson.GetNamedString("DevHomeSettingsCardHeader");
        }

        if (inputJson.ContainsKey("DevHomeSettingsCardHeaderIcon"))
        {
            adaptiveSettingsCard.HeaderIcon = inputJson.GetNamedString("DevHomeSettingsCardHeaderIcon");
        }

        if (inputJson.ContainsKey("DevHomeSettingsCardActionElement"))
        {
            var actionElementJson = inputJson.GetNamedObject("DevHomeSettingsCardActionElement");
            var actionElementType = actionElementJson.GetNamedString("type");

            // More action types can be added in the future by adding more cases here. Note this parser
            // will only parse elements that don't submit the adaptive card. If we need to support elements
            // that submit the adaptive card, we'll need to add a new parser action parser  and add the IAdaptiveActionElement
            // to the DevHomeSettingsCard's ActionElement property.
            if (string.Equals(actionElementType, DevHomeLaunchContentDialogButton.AdaptiveElementType, StringComparison.OrdinalIgnoreCase))
            {
                adaptiveSettingsCard.NonActionElement = CreateLaunchContentDialogButton(actionElementJson, elementParsers, actionParsers, warnings);
            }
            else
            {
                // If the action isn't one of our custom actions, we'll check if there is a built-in action parser that can parse it.
                var elementParser = elementParsers.Get(actionElementType);
                if (elementParser != null)
                {
                    adaptiveSettingsCard.NonActionElement = elementParser.FromJson(actionElementJson, elementParsers, actionParsers, warnings);
                }
            }
        }

        return adaptiveSettingsCard;
    }

    private DevHomeLaunchContentDialogButton? CreateLaunchContentDialogButton(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var parser = actionParsers.Get(DevHomeLaunchContentDialogButton.AdaptiveElementType);
        return parser.FromJson(inputJson, elementParsers, actionParsers, warnings) as DevHomeLaunchContentDialogButton;
    }
}
