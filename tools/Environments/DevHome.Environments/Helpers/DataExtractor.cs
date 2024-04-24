// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using DevHome.Environments.ViewModels;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Helpers;

public class DataExtractor
{
    private static StringResource _stringResource = new("DevHome.Environments.pri", "DevHome.Environments/Resources");

    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the dot button.
    /// ToDo: Use resources instead of literals
    /// ToDo: Add a pause after each operation
    /// </summary>
    /// <param name="computeSystem">Compute system used to fill OperationsViewModel's callback function.</param>
    public static List<OperationsViewModel> FillDotButtonOperations(ComputeSystem computeSystem, WinUIEx.WindowEx windowEx)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Restart))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Restart"), "\uE777", computeSystem.RestartAsync, windowEx));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Delete))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Delete"), "\uE74D", computeSystem.DeleteAsync, windowEx));
        }

        // ToDo: Correct the function used
        // operations.Add(new OperationsViewModel("Pin To Taskbar", "\uE718", computeSystem.));
        // operations.Add(new OperationsViewModel("Add to Start Menu", "\uE8A9", computeSystem.));
        return operations;
    }

    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the launch button.
    /// </summary>
    // <param name="computeSystem">Compute system used to fill OperationsViewModel's callback function.</param>
    public static List<OperationsViewModel> FillLaunchButtonOperations(ComputeSystem computeSystem)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Start))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Start"), "\uE768", computeSystem.StartAsync, null));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.ShutDown))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Stop"), "\uE71A", computeSystem.TerminateAsync, null));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.CreateSnapshot))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Checkpoint"), "\uE7C1", computeSystem.CreateSnapshotAsync, null));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.RevertSnapshot))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Revert"), "\uE7A7", computeSystem.RevertSnapshotAsync, null));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Pause))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Pause"), "\uE769", computeSystem.PauseAsync, null));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Resume))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Resume"), "\uE768", computeSystem.ResumeAsync, null));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Terminate))
        {
            operations.Add(new OperationsViewModel(_stringResource.GetLocalized("Operations_Terminate"), "\uEE95", computeSystem.TerminateAsync, null));
        }

        return operations;
    }
}
