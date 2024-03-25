// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public class DevHomeAdaptiveContentDialog : DevHomeAdaptiveCardElementBase, IDevHomeAdaptiveContentDialog
{
    public string Title { get; set; } = string.Empty;

    public string AdaptiveCardJsonTemplate { get; set; } = string.Empty;

    public string AdaptiveCardJsonData { get; set; } = string.Empty;

    public string PrimaryButtonText { get; set; } = string.Empty;

    public string SecondaryButtonText { get; set; } = string.Empty;

    public new string ElementTypeString { get; set; } = AdaptiveSettingsCardType;

    public static string AdaptiveSettingsCardType => "DevHome.AdaptiveContentDialog";
}
