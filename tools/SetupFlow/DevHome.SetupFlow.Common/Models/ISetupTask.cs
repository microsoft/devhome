﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.SetupFlow.ElevatedComponent;
using Windows.Foundation;

namespace DevHome.SetupFlow.Common.Models;

/// <summary>
/// A single atomic task to perform during the setup flow.
/// </summary>
public interface ISetupTask
{
    /// <summary>
    /// Gets a value indicating whether this task requires admin privileges to be executed.
    /// </summary>
    public abstract bool RequiresAdmin
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether this task requires to reboot the computer to be completed.
    /// </summary>
    /// <remarks>
    /// This will be used to guide whether we show a warning to the user about possible reboots
    /// before beginning the setup.
    /// TODO: We need to figure a story around how to handle reboots and the different cases.
    ///       Setting up WSL (future) will require us to reboot the machine to finish, but other
    ///       tasks like installing an app may trigger a reboot out of our control.
    /// </remarks>
    public abstract bool RequiresReboot
    {
        get;
    }

    /// <summary>
    /// Executes this setup task.
    /// </summary>
    /// <remarks>
    /// The task must work correctly in a background thread (not the UI thread).
    /// TODO: Define return type to report status in Loading page (success, failure)
    /// TODO: Define progress type to report status in Loading page
    /// TODO: We will have a background process to run tasks that require elevated permissions.
    ///       We will have to figure a way to communicate those tasks to the background process.
    /// </remarks>
    /// <returns>
    /// The async operation that executes this task. The value returned indicates whether the task completed successfully.
    /// </returns>
    public abstract IAsyncOperation<TaskFinishedState> Execute();

    /// <summary>
    /// Executes this setup task as admin.
    /// </summary>
    /// <param name="elevatedComponentFactory">Helper object to create the needed objects on the elevated process.</param>
    /// <returns>
    /// The async operation that executes this task. The value returned indicates whether the task completed successfully.
    /// </returns>
    public abstract IAsyncOperation<TaskFinishedState> ExecuteAsAdmin(IElevatedComponentFactory elevatedComponentFactory);

    /// <summary>
    /// Gets a string to show in the loading page while executing this task.
    /// </summary>
    /// <remarks>
    /// This method is called before a task execution to resolve <see cref="LoadingMessages.Executing"/>
    /// and again after a task execution to resolve the final message
    /// </remarks>
    /// <returns>A localized string indicating that this task is being executed.</returns>
    public abstract LoadingMessages GetLoadingMessages();
}
