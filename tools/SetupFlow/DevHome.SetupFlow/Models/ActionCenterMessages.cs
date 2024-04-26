// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Views;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Messages to show in the action center part of the loading screen when an item encountered an error
/// </summary>
public class ActionCenterMessages
{
    /// <summary>
    /// Gets or sets the message to show to the user
    /// </summary>
    public string PrimaryMessage
    {
        get; set;
    }

    public bool ShouldShowAdaptiveCardPanel => ExtensionAdaptiveCardPanel is not null;

    public bool DoesThisHaveActionableItems { get; }

    public ExtensionAdaptiveCardPanel ExtensionAdaptiveCardPanel { get; set; }

    public ActionCenterMessages(ExtensionAdaptiveCardPanel panel, string primaryMessage, bool doesThisHaveAnyActionableItems)
    {
        ExtensionAdaptiveCardPanel = panel;
        PrimaryMessage = primaryMessage;
        DoesThisHaveActionableItems = doesThisHaveAnyActionableItems;
    }

    public ActionCenterMessages()
    {
    }
}
