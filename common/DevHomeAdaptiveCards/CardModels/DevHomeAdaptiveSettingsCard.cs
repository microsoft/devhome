// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class DevHomeAdaptiveSettingsCard : DevHomeAdaptiveCardElementBase, IDevHomeAdaptiveSettingsCard
{
    // These properties relate to the Windows Community Toolkit's SettingsCard control.
    // We'll allow extensions to provide the data for the SettingsCard control from an Adaptive Card.
    // Then we'll render the actual SettingsCard control in the DevHome app.
    public string Description { get; set; } = string.Empty;

    public string Header { get; set; } = string.Empty;

    public string HeaderIcon { get; set; } = string.Empty;

    public IDevHomeAdaptiveSettingsCardAction? ActionElement { get; set; }

    public new string ElementTypeString { get; set; } = AdaptiveSettingsCardType;

    public static string AdaptiveSettingsCardType => "DevHome.AdaptiveSettingsCard";
}
