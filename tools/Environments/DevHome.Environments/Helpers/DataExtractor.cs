// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using DevHome.Environments.Models;
using DevHome.Environments.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.Helpers;

public class DataExtractor
{
    private static readonly StringResource _stringResource = new("DevHome.Environments.pri", "DevHome.Environments/Resources");

    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the dot button.
    /// ToDo: Use resources instead of literals
    /// ToDo: Add a pause after each operation
    /// </summary>
    /// <param name="computeSystem">Compute system used to fill OperationsViewModel's callback function.</param>
    /// <param name="window">The window object used for operations that require presenting UI.</param>
    public static List<OperationsViewModel> FillDotButtonOperations(ComputeSystemCache computeSystem, Window window)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations.Value;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Restart))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Restart"), "\uE777", computeSystem.RestartAsync, ComputeSystemOperations.Restart));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Delete))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Delete"), "\uE74D", computeSystem.DeleteAsync, ComputeSystemOperations.Delete, window));
        }

        return operations;
    }

    public static async Task<List<PinOperationData>> FillDotButtonPinOperationsAsync(ComputeSystemCache computeSystem)
    {
        var supportedOperations = computeSystem.SupportedOperations.Value;
        var operationData = new List<PinOperationData>();
        if (supportedOperations.HasFlag(ComputeSystemOperations.PinToTaskbar))
        {
            var pinResultTaskbar = await computeSystem.GetIsPinnedToTaskbarAsync();
            OperationsViewModel? operation = null;
            if (pinResultTaskbar.Result.Status == ProviderOperationStatus.Success)
            {
                if (pinResultTaskbar.IsPinned)
                {
                    var itemText = _stringResource.GetLocalized("UnpinFromTaskbarButtonContextMenuItem");
                    operation = new OperationsViewModel(itemText, "\uE77A", computeSystem.UnpinFromTaskbarAsync, ComputeSystemOperations.PinToTaskbar, "Unpin");
                }
                else
                {
                    var itemText = _stringResource.GetLocalized("PinToTaskbarButtonContextMenuItem");
                    operation = new OperationsViewModel(itemText, "\uE718", computeSystem.PinToTaskbarAsync, ComputeSystemOperations.PinToTaskbar, "Pin");
                }
            }

            operationData.Add(new(operation, pinResultTaskbar));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.PinToStartMenu))
        {
            var pinResultStartMenu = await computeSystem.GetIsPinnedToStartMenuAsync();
            OperationsViewModel? operation = null;
            if (pinResultStartMenu.Result.Status == ProviderOperationStatus.Success)
            {
                if (pinResultStartMenu.IsPinned)
                {
                    var itemText = _stringResource.GetLocalized("UnpinFromStartButtonContextMenuItem");
                    operation = new OperationsViewModel(itemText, "\uE77A", computeSystem.UnpinFromStartMenuAsync, ComputeSystemOperations.PinToStartMenu, "Unpin");
                }
                else
                {
                    var itemText = _stringResource.GetLocalized("PinToStartButtonContextMenuItem");
                    operation = new OperationsViewModel(itemText, "\uE718", computeSystem.PinToStartMenuAsync, ComputeSystemOperations.PinToStartMenu, "Pin");
                }
            }

            operationData.Add(new(operation, pinResultStartMenu));
        }

        return operationData;
    }

    /// <summary>
    /// Checks for supported operations and adds the text, icon, and function associated with the operation.
    /// Returns the list of operations to be added to the launch button.
    /// </summary>
    // <param name="computeSystem">Compute system used to fill OperationsViewModel's callback function.</param>
    public static List<OperationsViewModel> FillLaunchButtonOperations(ComputeSystemCache computeSystem)
    {
        var operations = new List<OperationsViewModel>();
        var supportedOperations = computeSystem.SupportedOperations.Value;

        if (supportedOperations.HasFlag(ComputeSystemOperations.Start))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Start"), "\uE768", computeSystem.StartAsync, ComputeSystemOperations.Start));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.ShutDown))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_ShutDown"), "\uE71A", computeSystem.ShutDownAsync, ComputeSystemOperations.ShutDown));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Save))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Save"), "\uE74E", computeSystem.SaveAsync, ComputeSystemOperations.Save));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.CreateSnapshot))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_CreateCheckpoint"), "\uE7C1", computeSystem.CreateSnapshotAsync, ComputeSystemOperations.CreateSnapshot));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.RevertSnapshot))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_RevertCheckpoint"), "\uE7A7", computeSystem.RevertSnapshotAsync, ComputeSystemOperations.RevertSnapshot));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Pause))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Pause"), "\uE769", computeSystem.PauseAsync, ComputeSystemOperations.Pause));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Resume))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Resume"), "\uE768", computeSystem.ResumeAsync, ComputeSystemOperations.Resume));
        }

        if (supportedOperations.HasFlag(ComputeSystemOperations.Terminate))
        {
            operations.Add(new OperationsViewModel(
                _stringResource.GetLocalized("Operations_Terminate"), "\uEE95", computeSystem.TerminateAsync, ComputeSystemOperations.Terminate));
        }

        return operations;
    }
}
