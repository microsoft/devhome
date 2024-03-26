// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public interface IDevHomeSettingsCard : IAdaptiveCardElement
{
    public string Description { get; set; }

    public string Header { get; set; }

    public string HeaderIcon { get; set; }

    // An element that is not expected to submit the adaptive card
    public IAdaptiveCardElement? NonActionElement { get; set; }

    // An element that is expected to submit the adaptive card
    public IAdaptiveActionElement? ActionElement { get; set; }
}
