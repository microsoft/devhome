// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Models;

namespace DevHome.SetupFlow.Models.Environments;

public class AdaptiveCardDataLoaded
{
    public RenderedAdaptiveCard RenderedAdaptiveCard { get; private set; }

    public string ErrorMessage { get; private set; }

    public AdaptiveCardDataLoaded(RenderedAdaptiveCard renderedAdaptiveCard, string errorMessage)
    {
        RenderedAdaptiveCard = renderedAdaptiveCard;
        ErrorMessage = errorMessage;
    }
}
