// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AdaptiveCards.Rendering.WinUI3;

namespace DevHome.Common.Contracts;

/// <summary>
/// Represents a renderer for an adaptive card action set that Dev Home can use to invoke actions from within
/// the action set. This is useful for invoking an adaptive card action from an abitraty button within Dev Home's UI.
/// </summary>
public interface IDevHomeActionSetRender : IAdaptiveElementRenderer
{
    /// <summary>
    /// Attempts to validate the user inputs and initiate the action.
    /// </summary>
    /// <param name="buttonId">The id of the button within the adaptive card Json template</param>
    /// <param name="userInputs">The user input from the adaptive card session</param>
    /// <returns>True if the users inputs were validated and false otherwise</returns>
    public bool TryValidateAndInitiateAction(string buttonId, AdaptiveInputs userInputs);

    /// <summary>
    /// Initiates the action without validating the user inputs.
    /// </summary>
    /// <param name="buttonId">The id of the button within the adaptive card Json template</param>
    public void InitiateAction(string buttonId);
}
