// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Renderers;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Responsible for moving forward and backwards in an adaptive card wizard flow.
/// </summary>
public class AdaptiveCardFlowNavigator
{
    /// <summary>
    /// Used by an adaptive card actionSet for the Id of the button that is used to move the flow forward.
    /// </summary>
    private readonly string _nextButtonAdaptiveCardId = "DevHomeMachineConfigurationNextButton";

    /// <summary>
    /// Used by an adaptive card actionSet for the Id of the button that is used to move the flow backwards.
    /// </summary>
    private readonly string _previousButtonAdaptiveCardId = "DevHomeMachineConfigurationPreviousButton";

    /// <summary>
    /// Gets the renderer for the Dev Home action set. This is used to invoke the action buttons within the
    /// top level action set of the adaptive card. It stitches together the setup flow's next and previous
    /// buttons to two buttons within an extensions adaptive card.
    /// </summary>
    /// <remarks>
    /// The Ids of the buttons in the extension's adaptive card must match the values in
    /// <see cref="_nextButtonAdaptiveCardId"/> and <see cref="_previousButtonAdaptiveCardId"/> in order for
    /// Dev Home to know which buttons in the adaptive card Json moves the flow forward and backwards.
    /// </remarks>
    public DevHomeActionSet DevHomeActionSetRenderer { get; } = new(TopLevelCardActionSetVisibility.Hidden);

    /// <summary>
    /// Performs the validation work needed to navigate to the next page in an adaptive card. This is used
    /// when the setup flow is rendering a flow that includes an adaptive card wizard flow.
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
