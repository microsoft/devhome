// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.ElevatedComponent;

namespace DevHome.SetupFlow.Common.Services;

/// <summary>
/// Orchestrator for the Setup Flow, in charge functionality across multiple pages.
/// </summary>
public class SetupFlowOrchestrator
{
    /// <summary>
    /// Relay commands associated with the navigation buttons in the UI.
    /// </summary>
    private readonly List<IRelayCommand> _navigationButtonsCommands = new ();

    /// <summary>
    /// Gets or sets the task groups that are to be executed on the flow.
    /// </summary>
    public IList<ISetupTaskGroup> TaskGroups
    {
        get; set;
    }

    public RemoteObject<IElevatedComponentFactory> RemoteElevatedFactory
    {
        get; set;
    }

    /// <summary>
    /// Sets the list of commands associated with the navigation buttons.
    /// </summary>
    public void SetNavigationButtonsCommands(IList<IRelayCommand> navigationButtonsCommands)
    {
        _navigationButtonsCommands.Clear();
        _navigationButtonsCommands.AddRange(navigationButtonsCommands);
    }

    /// <summary>
    /// Notify all the navigation buttons that the CanExecute property has changed.
    /// </summary>
    /// <remarks>
    /// This is used so that the individual pages can notify the navigation container
    /// about changes in state without having to reach into the navigation container.
    /// We could notify each button specifically, but this is simpler and not too bad.
    /// </remarks>
    public void NotifyNavigationCanExecuteChanged()
    {
        foreach (var command in _navigationButtonsCommands)
        {
            command.NotifyCanExecuteChanged();
        }
    }
}
