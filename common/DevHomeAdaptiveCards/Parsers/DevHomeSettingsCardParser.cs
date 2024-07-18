// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardInterfaces;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Environments.Helpers;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.Parsers;

public class DevHomeSettingsCardParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var adaptiveSettingsCard = new DevHomeSettingsCard();
        bool isCorrectType;

        if (inputJson.TryGetValue("id", out var id))
        {
            isCorrectType = id.ValueType == JsonValueType.String;
            adaptiveSettingsCard.Id = isCorrectType ? id.GetString() : string.Empty;
        }

        if (inputJson.TryGetValue("DevHomeSettingsCardDescription", out var devHomeSettingsCardDescription))
        {
            isCorrectType = devHomeSettingsCardDescription.ValueType == JsonValueType.String;
            adaptiveSettingsCard.Description = isCorrectType ? devHomeSettingsCardDescription.GetString() : StringResourceHelper.GetResource("SettingsCardDescriptionError");
        }

        if (inputJson.TryGetValue("DevHomeSettingsCardHeader", out var devHomeSettingsCardHeader))
        {
            isCorrectType = devHomeSettingsCardHeader.ValueType == JsonValueType.String;
            adaptiveSettingsCard.Header = isCorrectType ? devHomeSettingsCardHeader.GetString() : StringResourceHelper.GetResource("SettingsCardHeaderError");
        }

        if (inputJson.TryGetValue("DevHomeSettingsCardHeaderIcon", out var devHomeSettingsCardHeaderIcon))
        {
            isCorrectType = devHomeSettingsCardHeaderIcon.ValueType == JsonValueType.String;
            adaptiveSettingsCard.HeaderIcon = isCorrectType ? devHomeSettingsCardHeaderIcon.GetString() : string.Empty;
        }

        if (inputJson.TryGetValue("DevHomeSettingsCardActionElement", out var devHomeSettingsCardActionElement))
        {
            isCorrectType = devHomeSettingsCardActionElement.ValueType == JsonValueType.Object;
            var elementJson = isCorrectType ? devHomeSettingsCardActionElement.GetObject() : new JsonObject();
            var elementType = elementJson.GetNamedString("type", string.Empty);

            // More action types can be added in the future by adding more cases here. Note this parser
            // will only parse elements that don't submit the adaptive card. If we need to support elements
            // that submit the adaptive card, we'll need to add a new parser action parser  and add the IAdaptiveActionElement
            // to the DevHomeSettingsCard's ActionElement property.
            if (string.Equals(elementType, DevHomeLaunchContentDialogButton.AdaptiveElementType, StringComparison.OrdinalIgnoreCase))
            {
                adaptiveSettingsCard.NonSubmitActionElement = CreateLaunchContentDialogButton(elementJson, elementParsers, actionParsers, warnings);
            }
            else
            {
                // If the action isn't one of our custom actions, we'll check if there is a built-in action parser that can parse it.
                var elementParser = elementParsers.Get(elementType);
                if (elementParser != null)
                {
                    adaptiveSettingsCard.NonSubmitActionElement = elementParser.FromJson(elementJson, elementParsers, actionParsers, warnings) as IDevHomeSettingsCardNonSubmitAction;
                }
            }
        }

        return adaptiveSettingsCard;
    }

    private DevHomeLaunchContentDialogButton? CreateLaunchContentDialogButton(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var parser = elementParsers.Get(DevHomeLaunchContentDialogButton.AdaptiveElementType);
        return parser?.FromJson(inputJson, elementParsers, actionParsers, warnings) as DevHomeLaunchContentDialogButton;
    }
}
