// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public interface IAdaptiveCardSettingsCard
{
    public string Description { get; set; } = string.Empty;

    public string SubDescription { get; set; } = string.Empty;

    public string IconElement { get; set; }

    public string InnerAdaptiveCardJsonTemplate { get; set; }

    public string InnerAdaptiveCardJsonData { get; set; }

    public string InnerAdaptiveCardTitle { get; set; } = string.Empty;

    public string ActionButtonText { get; set; } = string.Empty;

    public bool ShouldShowActionItem { get; set; } = true;
}
