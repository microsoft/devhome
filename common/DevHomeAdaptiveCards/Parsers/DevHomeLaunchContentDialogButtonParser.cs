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

public class DevHomeLaunchContentDialogButtonParser : IAdaptiveElementParser
{
    public IAdaptiveCardElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var action = new DevHomeLaunchContentDialogButton();
        if (inputJson.ContainsKey("DevHomeActionText"))
        {
            action.ActionText = inputJson.GetNamedString("DevHomeActionText");
        }

        // Parse the content dialog element and place its content into our content dialog button property.
        if (inputJson.ContainsKey("DevHomeContentDialogContent"))
        {
            var contentDialogJson = inputJson.GetNamedObject("DevHomeContentDialogContent");
            var contentDialogParser = elementParsers.Get(DevHomeContentDialogContent.AdaptiveElementType);
            action.DialogContent = contentDialogParser.FromJson(contentDialogJson, elementParsers, actionParsers, warnings);
        }

        return action;
    }
}
