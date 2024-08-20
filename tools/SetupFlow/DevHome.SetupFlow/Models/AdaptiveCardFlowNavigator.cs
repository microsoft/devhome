// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Renderers;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Responsible for moving forwards and backwards in an adaptive card flow in the setup flow.
/// </summary>
public class AdaptiveCardFlowNavigator
{
    /// <summary>
    /// Used by adaptive card actionSet to distinguish which button be used to move the flow forward.
    /// </summary>
    private readonly string _nextButtonAdaptiveCardId = "DevHomeMachineConfigurationNextButton";

    /// <summary>
    /// Used by adaptive card actionSet to distinguish which button be used to move the flow backwards.
    /// </summary>
    private readonly string _previousButtonAdaptiveCardId = "DevHomeMachineConfigurationPreviousButton";

    /// <summary>
    /// Gets the renderer for the Dev Home action set. This is used to invoke the action buttons within the
    /// top level action set of the adaptive card. It stitches together the setup flow's next and previous
    /// buttons to two buttons within an extensions adaptive card.
    /// </summary>
    public DevHomeActionSet DevHomeActionSetRenderer { get; } = new(TopLevelCardActionSetVisibility.Hidden);

    /// <summary>
    /// Performs the validation work needed to navigate to the next page in an adaptive card. This is used
    /// when the setup flow is rendering a flow that includes an adaptive card style wizard flow.
    /// </summary>
    /// <remarks>
    /// Only adaptive cards that have input controls with the 'isRequired' property set to true will be validated.
    /// All other elements within the adaptive card will be ignored. When the inputs are validated the adaptive card
    /// action is invoked.
    /// </remarks>
    /// <returns>True when the user inputs have been validated and false otherwise.</returns>
    public bool GoToNextPageWithValidation(AdaptiveInputs userInputs)
    {
        return DevHomeActionSetRenderer.TryValidateAndInitiateAction(_nextButtonAdaptiveCardId, userInputs);
    }

    public void GoToNextPage()
    {
        DevHomeActionSetRenderer.InitiateAction(_nextButtonAdaptiveCardId);
    }

    public void GoToPreviousPage()
    {
        DevHomeActionSetRenderer.InitiateAction(_previousButtonAdaptiveCardId);
    }

    public bool IsActionButtonIdNextButtonId(string buttonId)
    {
        return _nextButtonAdaptiveCardId.Equals(buttonId, System.StringComparison.OrdinalIgnoreCase);
    }

    public bool IsActionInvokerAvailable()
    {
        return DevHomeActionSetRenderer.ActionButtonInvoker != null;
    }
}
