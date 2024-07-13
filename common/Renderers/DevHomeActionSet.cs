// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
/// Represents a button in the top level action set in the adaptive card json.
/// We'll provide guidance to adaptive card creators to put one or two actions in the top level action set.
/// if they want to give Dev Home the ability to submit/execute their adaptive card button from
/// within Dev Home.
/// </summary>
public enum ActionButtonToInvoke
{
    Primary,
    Secondary,
}

/// <summary>
/// Allows Dev Home to attach an action to a button in the Dev Home UI.
/// Dev Home can use this invoke an action from an adaptive card when
/// a button within Dev Home is clicked. It allows Dev Home to link a primary
/// and secondary button to two actions within a top level action set in the adaptive card.
/// </summary>
/// <remarks>
/// It is expected that the adaptive card will have a top level action set with two actions.
/// for Dev Home to link to. The primary button in Dev Home will be linked to the first action
/// and the secondary button will be linked to the second action.
/// </remarks>
public class DevHomeActionSet : IDevHomeActionRender
{
    /// <summary>
    /// Gets the visibility of the action in Dev Home's UI.
    /// </summary>
    private readonly TopLevelCardActionSetVisibility _actionVisibility;

    /// <summary>
    /// Gets the adaptive card object that will invoke an action within the action set.
    /// </summary>
    public AdaptiveActionInvoker? ActionButtonInvoker { get; private set; }

    private IAdaptiveActionElement? _primaryButtonAdaptiveActionElement;

    private IAdaptiveActionElement? _secondaryButtonAdaptiveActionElement;

    public DevHomeActionSet(TopLevelCardActionSetVisibility cardActionVisibility)
    {
        _actionVisibility = cardActionVisibility;
    }

    public UIElement? Render(IAdaptiveCardElement element, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        if (element is AdaptiveActionSet actionSet)
        {
            // We'll only support two submit/execute actions in the action set.
            // one for the primary action and one for the secondary action.
            if (actionSet.Actions.Count > 2 || actionSet.Actions.Count == 0)
            {
                throw new ArgumentException("The ActionSet must have a minimum of 1 action and maximum of 2 actions.");
            }

            ActionButtonInvoker = context.ActionInvoker;

            for (var i = 0; i < actionSet.Actions.Count; i++)
            {
                var action = actionSet.Actions[i];
                if (action is AdaptiveExecuteAction executeAction)
                {
                    LinkSubmitActionToCard(i, executeAction, context, renderArgs);
                }
                else if (action is AdaptiveSubmitAction submitAction)
                {
                    LinkSubmitActionToCard(i, submitAction, context, renderArgs);
                }
            }

            if (_actionVisibility == TopLevelCardActionSetVisibility.Hidden)
            {
                // the page in Dev Home does not want to show the action set in the card.
                // but needed to link a button in Dev Home to the action. So we return null,
                // so we don't render the action set. Invoking the button from Dev Home will
                // then trigger the action in the adaptive card.
                return null;
            }
        }

        var renderer = new AdaptiveActionSetRenderer();
        return renderer.Render(element, context, renderArgs);
    }

    /// <summary>
    /// Invokes an adaptive card action from another control like a button in Dev Home.
    /// </summary>
    /// <param name="buttonToInvoke">The primary or seconday enum value that will be used to choose which one Dev Home will invoke</param>
    public void InitiateAction(ActionButtonToInvoke buttonToInvoke)
    {
        IAdaptiveActionElement? adaptiveActionElement = _primaryButtonAdaptiveActionElement;

        if (buttonToInvoke == ActionButtonToInvoke.Primary)
        {
            adaptiveActionElement = _secondaryButtonAdaptiveActionElement;
        }

        ActionButtonInvoker?.SendActionEvent(adaptiveActionElement);
    }

    /// <summary>
    /// Links the submit/execute actiono to the adaptive card itself.
    /// </summary>
    private void LinkSubmitActionToCard(int index, IAdaptiveActionElement action, AdaptiveRenderContext context, AdaptiveRenderArgs renderArgs)
    {
        if (index == 0)
        {
            context.LinkSubmitActionToCard(action, renderArgs);
            _primaryButtonAdaptiveActionElement = action;
        }
        else
        {
            context.LinkSubmitActionToCard(action, renderArgs);
            _secondaryButtonAdaptiveActionElement = action;
        }
    }
}
