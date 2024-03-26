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

public class DevHomeContentDialogContentParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var dialog = new DevHomeContentDialogContent();

        if (inputJson.TryGetValue("DevHomeContentDialogTitle", out var titleJsonValue))
        {
            dialog.Title = titleJsonValue.GetString();
        }

        if (inputJson.TryGetValue("DevHomeContentDialogContainerElement", out var adaptiveCardContainerElement))
        {
            // Parse the container element and pass it to the dialog. This will be the content of the dialog.
            var containerElementParser = elementParsers.Get("Adaptive.Container");
            dialog.ContainerElement = containerElementParser.FromJson(adaptiveCardContainerElement.GetObject(), elementParsers, actionParsers, warnings);
        }

        if (inputJson.TryGetValue("DevHomeContentDialogPrimaryButtonText", out var primaryButtonTextJsonValue))
        {
            dialog.PrimaryButtonText = primaryButtonTextJsonValue.GetString();
        }

        if (inputJson.TryGetValue("DevHomeContentDialogSecondaryButtonText", out var secondaryButtonTextJsonValue))
        {
            dialog.SecondaryButtonText = secondaryButtonTextJsonValue.GetString();
        }

        return dialog;
    }
}
