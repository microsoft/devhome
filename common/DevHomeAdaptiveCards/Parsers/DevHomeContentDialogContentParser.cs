// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using DevHome.Common.DevHomeAdaptiveCards.CardModels;
using DevHome.Common.Environments.Helpers;
using Windows.Data.Json;

namespace DevHome.Common.DevHomeAdaptiveCards.Parsers;

/// <summary>
/// Represents a parser for a Dev Home content dialog card that can be rendered through an adaptive card.
/// This parser will be used if the element type is "DevHome.ContentDialogContent".
/// </summary>
/// <remarks>
/// the JsonObject is a Windows.Data.Json.JsonObject, which has methods that can throw an exception if the type is not correct.
/// </remarks>
public class DevHomeContentDialogContentParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var dialog = new DevHomeContentDialogContent();
        bool isCorrectType;

        if (inputJson.TryGetValue("devHomeContentDialogTitle", out var devHomeContentDialogTitle))
        {
            isCorrectType = devHomeContentDialogTitle.ValueType == JsonValueType.String;
            dialog.Title = isCorrectType ? devHomeContentDialogTitle.GetString() : StringResourceHelper.GetResource("DevHomeContentDialogDefaultTitle");
        }

        if (inputJson.TryGetValue("devHomeContentDialogBodyAdaptiveCard", out var contentDialogInternalAdaptiveCardJson))
        {
            isCorrectType = contentDialogInternalAdaptiveCardJson.ValueType == JsonValueType.Object;
            dialog.ContentDialogInternalAdaptiveCardJson = isCorrectType ? contentDialogInternalAdaptiveCardJson.GetObject() : new JsonObject();
        }

        if (inputJson.TryGetValue("devHomeContentDialogPrimaryButtonText", out var devHomeContentDialogPrimaryButtonText))
        {
            isCorrectType = devHomeContentDialogPrimaryButtonText.ValueType == JsonValueType.String;
            dialog.PrimaryButtonText = isCorrectType ? devHomeContentDialogPrimaryButtonText.GetString() : StringResourceHelper.GetResource("DevHomeContentDialogDefaultPrimaryButtonText");
        }

        if (inputJson.TryGetValue("devHomeContentDialogSecondaryButtonText", out var devHomeContentDialogSecondaryButtonText))
        {
            isCorrectType = devHomeContentDialogSecondaryButtonText.ValueType == JsonValueType.String;
            dialog.SecondaryButtonText = isCorrectType ? devHomeContentDialogSecondaryButtonText.GetString() : StringResourceHelper.GetResource("DevHomeContentDialogDefaultSecondaryButtonText");
        }

        return dialog;
    }
}
