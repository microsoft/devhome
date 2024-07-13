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

public class DevHomeAdaptiveSettingsCardActionParser : IAdaptiveActionParser
{
    public IAdaptiveActionElement? FromJson(JsonObject inputJson, AdaptiveElementParserRegistration elementParsers, AdaptiveActionParserRegistration actionParsers, IList<AdaptiveWarning> warnings)
    {
        var actionKind = GetActionKind(inputJson);
        if (actionKind == AdaptiveSettingsCardActionKind.Unknown)
        {
            return null;
        }

        // As of now, we only support LaunchContentDialog action, but more actions can be added in the future.
        if (actionKind == AdaptiveSettingsCardActionKind.LaunchContentDialog)
        {
            return CreateContentDialogLaunchAction(inputJson);
        }

        return null;
    }

    private DevHomeAdaptiveSettingsCardLaunchContentDialogButton CreateContentDialogLaunchAction(JsonObject inputJson)
    {
        var action = new DevHomeAdaptiveSettingsCardLaunchContentDialogButton();

        if (inputJson.TryGetValue("actionButtonText", out var actionButtonText))
        {
            action.ActionItemText = actionButtonText.GetString();
        }

        return action;
    }

    private AdaptiveSettingsCardActionKind GetActionKind(JsonObject inputJson)
    {
        if (inputJson.TryGetValue("actionKind", out var actionKind))
        {
            Enum.TryParse(typeof(AdaptiveSettingsCardActionKind), actionKind.GetString(), out var enumValue);

            if (enumValue != null)
            {
                try
                {
                    return (AdaptiveSettingsCardActionKind)enumValue;
                }
                catch (Exception)
                {
                }
            }
        }

        return AdaptiveSettingsCardActionKind.Unknown;
    }
}
