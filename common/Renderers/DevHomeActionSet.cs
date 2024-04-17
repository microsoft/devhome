// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveCards.ObjectModel.WinUI3;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Contracts;
using Microsoft.UI.Xaml;

namespace DevHome.Common.Renderers;

/// <summary>
/// Represents whether the adaptive card button in an action set should be visible or hidden in Dev Home's UI.
/// Although adaptive cards can natively hide elements, this is used on the Dev Home side to hide the action set
/// in cases where a page in Dev Home wants its own button to invoke the action in the adaptive card.
/// </summary>
public enum TopLevelCardActionSetVisibility
{
    Visible,
    Hidden,
}

/// <summary>
/// Allows Dev Home to attach an action to a button in the Dev Home UI.
/// Dev Home can use this to invoke an action from an adaptive card when
/// a button within Dev Home is clicked. It allows Dev Home to link buttons from
/// within its own UI the to top level action set in the adaptive card.
/// </summary>
/// <remarks>
/// It is expected that the adaptive card will have a top level action set with buttons that Dev Home can link to.
/// </remarks>
public class DevHomeActionSet : IDevHomeActionSetRender
{
    /// <summary>
    /// Gets the visibility of the action in Dev Home's UI.
    /// </summary>
    private readonly TopLevelCardActionSetVisibility _actionSetVisibility;

    private const string TopLevelCardActionId = "DevHomeTopLevelActionSet";

    /// <summary>
    /// Gets the adaptive card object that will invoke an action within the action set.
    /// </summary>
    public AdaptiveActionInvoker? ActionButtonInvoker { get; private set; }

    public AdaptiveCard? OriginalAdaptiveCard { get; private set; }

    private Dictionary<string, IAdaptiveActionElement> ActionButtonMap { get; } = new();

    public DevHomeActionSet(TopLevelCardActionSetVisibility cardActionSetVisibility)
    {
        _actionSetVisibility = cardActionSetVisibility;
    }

    public UIElement? Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        ActionButtonMap.Clear();

        if (element is AdaptiveActionSet actionSet)
        {
            foreach (var action in actionSet.Actions)
            {
                if (action is AdaptiveExecuteAction executeAction)
                {
                    context.LinkSubmitActionToCard(executeAction, renderArgs);
                }
                else if (action is AdaptiveSubmitAction submitAction)
                {
                    context.LinkSubmitActionToCard(submitAction, renderArgs);
                }

                ActionButtonMap.TryAdd(action.Id, action);
            }

            ActionButtonInvoker = context.ActionInvoker;
            OriginalAdaptiveCard = renderArgs.ParentCard;

            if (_actionSetVisibility == TopLevelCardActionSetVisibility.Hidden &&
                actionSet.Id.Equals(TopLevelCardActionId, System.StringComparison.OrdinalIgnoreCase))
            {
                // the page in Dev Home does not want to show the action set in the card.
                // So we return null to prevent the adaptive card buttons from appearing.
                // Invoking the button from Dev Home will then trigger the action in the adaptive card.
                return null;
            }
        }

        var renderer = new AdaptiveActionSetRenderer();
        return renderer.Render(element, context, renderArgs);
    }

    /// <summary>
    /// Invokes an adaptive card action from anywhere within Dev Home, like a method in a view Model for example.
    /// A boolean is returned with the validation result of the card. We still send the action event so the adaptive
    /// cards UI updates with error information.
    /// </summary>
    /// <returns>
    /// A boolean indicating whether validation for the card passed or failed.
    /// </returns>
    public bool TryValidateAndInitiateAction(string buttonId, AdaptiveInputs userInputs)
    {
        ActionButtonMap.TryGetValue(buttonId, out var actionElement);

        if ((actionElement == null) || (userInputs == null))
        {
            return false;
        }

        var result = userInputs.ValidateInputs(actionElement);

        ActionButtonInvoker?.SendActionEvent(actionElement);
        return result;
    }

    public void InitiateAction(string buttonId)
    {
        ActionButtonMap.TryGetValue(buttonId, out var actionElement);

        if (actionElement == null)
        {
            return;
        }

        ActionButtonInvoker?.SendActionEvent(actionElement);
    }

    public string GetActionTitle(string buttonId)
    {
        if (!ActionButtonMap.TryGetValue(buttonId, out var action))
        {
            return string.Empty;
        }

        return action.Title;
    }
}
