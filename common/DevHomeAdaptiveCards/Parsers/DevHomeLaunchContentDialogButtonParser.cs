// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Reflection.Emit;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Environments.Helpers;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.Parsers;

public class DevHomeLaunchContentDialogButtonParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var action = new DevHomeLaunchContentDialogButton();
        bool isCorrectType;

        if (inputJson.TryGetValue("DevHomeActionText", out var devHomeActionText))
        {
            isCorrectType = devHomeActionText.ValueType == JsonValueType.String;
            action.ActionText = isCorrectType ? devHomeActionText.GetString() : StringResourceHelper.GetResource("DevHomeActionDefaultText");
        }

        // Parse the content dialog element and place its content into our content dialog button property.
        if (inputJson.TryGetValue("DevHomeContentDialogContent", out var devHomeContentDialogContent))
        {
            isCorrectType = devHomeContentDialogContent.ValueType == JsonValueType.Object;
            var contentDialogJson = isCorrectType ? devHomeContentDialogContent.GetObject() : new JsonObject();
            var contentDialogParser = elementParsers.Get(DevHomeContentDialogContent.AdaptiveElementType);
            action.DialogContent = contentDialogParser?.FromJson(contentDialogJson, elementParsers, actionParsers, warnings);
        }

        return action;
    }
}
