// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Environments.Helpers;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.Parsers;

public class DevHomeContentDialogContentParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var dialog = new DevHomeContentDialogContent();
        bool isCorrectType;

        if (inputJson.TryGetValue("DevHomeContentDialogTitle", out var devHomeContentDialogTitle))
        {
            isCorrectType = devHomeContentDialogTitle.ValueType == JsonValueType.String;
            dialog.Title = isCorrectType ? devHomeContentDialogTitle.GetString() : StringResourceHelper.GetResource("DevHomeContentDialogDefaultTitle");
        }

        if (inputJson.TryGetValue("DevHomeContentDialogBodyAdaptiveCard", out var contentDialogInternalAdaptiveCardJson))
        {
            isCorrectType = contentDialogInternalAdaptiveCardJson.ValueType == JsonValueType.Object;
            dialog.ContentDialogInternalAdaptiveCardJson = isCorrectType ? contentDialogInternalAdaptiveCardJson.GetObject() : new JsonObject();
        }

        if (inputJson.TryGetValue("DevHomeContentDialogPrimaryButtonText", out var devHomeContentDialogPrimaryButtonText))
        {
            isCorrectType = devHomeContentDialogPrimaryButtonText.ValueType == JsonValueType.String;
            dialog.PrimaryButtonText = isCorrectType ? devHomeContentDialogPrimaryButtonText.GetString() : StringResourceHelper.GetResource("DevHomeContentDialogDefaultPrimaryButtonText");
        }

        if (inputJson.TryGetValue("DevHomeContentDialogSecondaryButtonText", out var devHomeContentDialogSecondaryButtonText))
        {
            isCorrectType = devHomeContentDialogSecondaryButtonText.ValueType == JsonValueType.String;
            dialog.SecondaryButtonText = isCorrectType ? devHomeContentDialogSecondaryButtonText.GetString() : StringResourceHelper.GetResource("DevHomeContentDialogDefaultSecondaryButtonText");
        }

        return dialog;
    }
}
