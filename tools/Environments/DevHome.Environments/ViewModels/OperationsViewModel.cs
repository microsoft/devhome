// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Environments.Models;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// OperationKind represents two different types of operations
/// the view model can perform.
/// 1. ExtensionTask: An environment extension's method that returns a task
/// 2. DevHomeAction: A Action that is internal to Dev Home. E.g an Action to Remove an item from the UI.
/// </summary>
public enum OperationKind
{
    ExtensionTask,
    DevHomeAction,
}

/// <summary>
/// Represents an operation that can be performed on a compute system.
/// This is used to populate the launch and dot buttons on the compute system card.
/// </summary>
public partial class OperationsViewModel
{
    private readonly OperationKind _operationKind;

    public string Name { get; }

    public ComputeSystemOperations ComputeSystemOperation { get; }

    public string IconGlyph { get; }

    private Func<string, Task<ComputeSystemOperationResult>>? ExtensionTask { get; }

    private Action? DevHomeAction { get; }

    public event TypedEventHandler<OperationsViewModel, ComputeSystemOperationStartedEventArgs>? OperationStarted;

    public event TypedEventHandler<OperationsViewModel, ComputeSystemOperationCompletedEventArgs>? OperationCompleted;

    public OperationsViewModel(string name, string icon, Func<string, Task<ComputeSystemOperationResult>> command, ComputeSystemOperations computeSystemOperation)
    {
        _operationKind = OperationKind.ExtensionTask;
        Name = name;
        IconGlyph = icon;
        ExtensionTask = command;
        ComputeSystemOperation = computeSystemOperation;
    }

    public OperationsViewModel(string name, string icon, Action command)
    {
        _operationKind = OperationKind.DevHomeAction;
        Name = name;
        IconGlyph = icon;
        DevHomeAction = command;
    }

    [RelayCommand]
    public void InvokeAction()
    {
        Task.Run(async () =>
        {
            if (_operationKind == OperationKind.DevHomeAction)
            {
                DevHomeAction!();
                return;
            }

            var activityId = Guid.NewGuid();
            OperationStarted?.Invoke(this, new ComputeSystemOperationStartedEventArgs(ComputeSystemOperation, activityId));
            var result = await ExtensionTask!(string.Empty);
            OperationCompleted?.Invoke(this, new ComputeSystemOperationCompletedEventArgs(ComputeSystemOperation, result, activityId));
        });
    }
}
