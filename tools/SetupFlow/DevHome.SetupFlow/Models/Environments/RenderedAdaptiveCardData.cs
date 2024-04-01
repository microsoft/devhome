// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.SetupFlow.Models.Environments;

public class RenderedAdaptiveCardData
{
    public object CurrentSetupFlowViewModel { get; private set; }

    public RenderedAdaptiveCard RenderedAdaptiveCard { get; set; }

    public RenderedAdaptiveCardData(object currentSetupFlowViewModel, RenderedAdaptiveCard renderedAdaptiveCard)
    {
        CurrentSetupFlowViewModel = currentSetupFlowViewModel;
        RenderedAdaptiveCard = renderedAdaptiveCard;
    }
}
