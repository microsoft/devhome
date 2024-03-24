// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using Microsoft.UI.Xaml;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class DevHomeAdaptiveSettingsCardLaunchContentDialogButton : DevHomeAdaptiveCardActionBase, IDevHomeAdaptiveSettingsCardAction
{
    public string ActionItemText { get; set; } = string.Empty;

    public AdaptiveSettingsCardActionKind ActionKind => AdaptiveSettingsCardActionKind.LaunchContentDialog;

    public void InvokeAction()
    {
    }

    public IDevHomeAdaptiveContentDialog? ContentDialogAdaptiveCard { get; set; }

    public new string ActionTypeString { get; set; } = AdaptiveSettingsCardActionType;

    public static string AdaptiveSettingsCardActionType => "DevHome.AdaptiveSettingsCardLaunchContentDialogButton";
}
