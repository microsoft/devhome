// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

public interface IDevHomeAdaptiveContentDialog : IAdaptiveCardElement
{
    public string Title { get; set; }

    public string AdaptiveCardJsonTemplate { get; set; }

    public string AdaptiveCardJsonData { get; set; }

    public string PrimaryButtonText { get; set; }

    public string SecondaryButtonText { get; set; }
}
