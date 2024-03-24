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
    Unknown,
    LaunchContentDialog,
}

public enum DevHomeAdaptiveSettingsCardActionControl
{
    Button,
}

public interface IDevHomeAdaptiveSettingsCardAction : IAdaptiveActionElement
{
    public string ActionItemText { get; }

    public AdaptiveSettingsCardActionKind ActionKind { get; }

    public void InvokeAction();
}
