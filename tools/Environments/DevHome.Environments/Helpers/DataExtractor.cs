// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Environments.Models;
using DevHome.Environments.ViewModels;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Helpers;

public class DataExtractor
{
    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the dot button.
    /// ToDo: Use resources instead of literals
    /// ToDo: Add a pause after each operation
    /// </summary>
    /// <param name="computeSystem">Compute system used to fill OperationsViewModel's callback function.</param>
    public static List<OperationsViewModel> FillDotButtonOperations(ComputeSystem computeSystem)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Restart))
        {
            operations.Add(new OperationsViewModel("Restart", "\uE777", computeSystem.RestartAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Delete))
        {
            operations.Add(new OperationsViewModel("Delete", "\uE74D", computeSystem.DeleteAsync));
        }

        // ToDo: Correct the function used
        // operations.Add(new OperationsViewModel("Pin To Taskbar", "\uE718", computeSystem.DeleteAsync));
        // operations.Add(new OperationsViewModel("Add to Start Menu", "\uF0DF", computeSystem.DeleteAsync));
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
            operations.Add(new OperationsViewModel("Start", "\uE768", computeSystem.StartAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.ShutDown))
        {
            operations.Add(new OperationsViewModel("Stop", "\uE71A", computeSystem.TerminateAsync));
        }

        // if (supportedOperations.HasFlag(ComputeSystemOperations.Schedule))
        // {
        //    operations.Add(new OperationsViewModel("Schedule", "\uEC92", computeSystem.ScheduleAsync));
        // }
        operations.Add(new OperationsViewModel("Schedule", "\uEC92", computeSystem.ScheduleAsync));

        if (supportedOperations.HasFlag(ComputeSystemOperations.CreateSnapshot))
        {
            operations.Add(new OperationsViewModel("Checkpoint", "\uE7C1", computeSystem.CreateSnapshotAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.RevertSnapshot))
        {
            operations.Add(new OperationsViewModel("Revert", "\uE7A7", computeSystem.RevertSnapshotAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Pause))
        {
            operations.Add(new OperationsViewModel("Pause", "\uE769", computeSystem.PauseAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Resume))
        {
            operations.Add(new OperationsViewModel("Resume", "\uF2C6", computeSystem.ResumeAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Terminate))
        {
            operations.Add(new OperationsViewModel("Terminate", "\uE71A", computeSystem.TerminateAsync));
        }

        return operations;
    }
}
