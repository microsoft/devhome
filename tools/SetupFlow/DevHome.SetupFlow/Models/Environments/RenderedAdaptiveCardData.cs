// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Models;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Data object that contains the rendered adaptive card and the current view model being used in
/// the setup flow by the <see cref="Services.SetupFlowOrchestrator"/>.
/// </summary>
public class RenderedAdaptiveCardData
{
    public object CurrentSetupFlowViewModel { get; private set; }

    public RenderedAdaptiveCard RenderedAdaptiveCard { get; set; }

    public string ErrorMessage { get; }

    public RenderedAdaptiveCardData(
        object currentSetupFlowViewModel,
        RenderedAdaptiveCard renderedAdaptiveCard,
        string errorMessage = null)
    {
        CurrentSetupFlowViewModel = currentSetupFlowViewModel;
        RenderedAdaptiveCard = renderedAdaptiveCard;
        ErrorMessage = errorMessage;
    }
}
