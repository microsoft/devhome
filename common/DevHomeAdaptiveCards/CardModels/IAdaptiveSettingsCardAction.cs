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

public enum AdaptiveSettingsCardActionKind
{
    None,
    LaunchContentDialog,
}

public interface IAdaptiveSettingsCardAction : IAdaptiveActionElement
{
    public string ActionButtonText { get; set; }

    public AdaptiveSettingsCardActionKind ActionKind { get; set; }

    public void LaunchContentDialog(AdaptiveCardRenderer adaptiveCardRenderer, );
}
