// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
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

        return operations;
    }

    public static async Task<List<OperationsViewModel>> FillDotButtonPinOperationsAsync(ComputeSystem computeSystem)
    {
        var supportedOperations = computeSystem.SupportedOperations;
        var operations = new List<OperationsViewModel>();
        if (supportedOperations.HasFlag(ComputeSystemOperations.PinToTaskbar))
        {
            var pinResultTaskbar = await computeSystem.GetIsPinnedToTaskbarAsync();
            if (pinResultTaskbar.Result.Status == ProviderOperationStatus.Success)
            {
                if (pinResultTaskbar.IsPinned)
                {
                    operations.Add(new OperationsViewModel("Unpin From Taskbar", "\uE74D", computeSystem.SetIsPinnedToTaskbarAsync, false));
                }
                else
                {
                    operations.Add(new OperationsViewModel("Pin To Taskbar", "\uE74D", computeSystem.SetIsPinnedToTaskbarAsync, true));
                }
            }
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.PinToStartMenu))
        {
            var pinResultStartMenu = await computeSystem.GetIsPinnedToStartMenuAsync();
            if (pinResultStartMenu.Result.Status == ProviderOperationStatus.Success)
            {
                if (pinResultStartMenu.IsPinned)
                {
                    operations.Add(new OperationsViewModel("Unpin From Start Menu", "\uE74D", computeSystem.SetIsPinnedToStartMenuAsync, false));
                }
                else
                {
                    operations.Add(new OperationsViewModel("Pin To Start Menu", "\uE74D", computeSystem.SetIsPinnedToStartMenuAsync, true));
                }
            }
        }

        // TODO: _log.Error($"Failed to get state for {ComputeSystem.DisplayName} due to {result.Result.DiagnosticText}");
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
            operations.Add(new OperationsViewModel("Resume", "\uE768", computeSystem.ResumeAsync));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Terminate))
        {
            operations.Add(new OperationsViewModel("Terminate", "\uEE95", computeSystem.TerminateAsync));
        }

        return operations;
    }
}
