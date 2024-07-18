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

public class DevHomeAdaptiveSettingsCardLaunchContentDialogButtonParser : IAdaptiveActionParser
{
    public IAdaptiveActionElement FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var action = new DevHomeAdaptiveSettingsCardLaunchContentDialogButton();
        if (inputJson.ContainsKey("actionItemText"))
        {
            action.ActionItemText = inputJson.GetNamedString("actionItemText");
        }

        if (inputJson.ContainsKey("DevHomeContentDialog"))
        {
            var contentDialogJson = inputJson.GetNamedObject("DevHomeContentDialog");
            action.ContentDialogAdaptiveCard = (IDevHomeAdaptiveContentDialog)elementParsers.Get(DevHomeAdaptiveContentDialog.AdaptiveSettingsCardType).FromJson(contentDialogJson, elementParsers, actionParsers, warnings);
        }

        return action;
    }
}
