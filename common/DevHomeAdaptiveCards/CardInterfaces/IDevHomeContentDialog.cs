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

public interface IDevHomeContentDialog : IAdaptiveCardElement
{
    public string Title { get; set; }

    // This should always be the container adaptive card element.
    // We'll use the container element as the content of the content dialog.
    public IAdaptiveCardElement? ContainerElement { get; set; }

    public string PrimaryButtonText { get; set; }

    public string SecondaryButtonText { get; set; }
}
