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

public class DevHomeAdaptiveContentDialogParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var dialog = new DevHomeAdaptiveContentDialog();

        if (inputJson.TryGetValue("title", out var titleJsonValue))
        {
            dialog.Title = titleJsonValue.GetString();
        }

        if (inputJson.TryGetValue("adaptiveCardJsonTemplate", out var adaptiveCardJsonTemplateJsonValue))
        {
            dialog.AdaptiveCardJsonTemplate = adaptiveCardJsonTemplateJsonValue.GetString();
        }

        if (inputJson.TryGetValue("adaptiveCardJsonData", out var adaptiveCardJsonDataJsonValue))
        {
            dialog.AdaptiveCardJsonData = adaptiveCardJsonDataJsonValue.GetString();
        }

        if (inputJson.TryGetValue("primaryButtonText", out var primaryButtonTextJsonValue))
        {
            dialog.PrimaryButtonText = primaryButtonTextJsonValue.GetString();
        }

        if (inputJson.TryGetValue("secondaryButtonText", out var secondaryButtonTextJsonValue))
        {
            dialog.SecondaryButtonText = secondaryButtonTextJsonValue.GetString();
        }

        return dialog;
    }
}
