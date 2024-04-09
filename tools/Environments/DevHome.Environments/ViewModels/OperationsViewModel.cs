// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.ViewModels;

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

    public string? Description { get; set; }

    public string IconGlyph { get; }

    private Func<string, Task<ComputeSystemOperationResult>>? ExtensionTask { get; }

    private Action? DevHomeAction { get; }

    public OperationsViewModel(string name, string icon, Func<string, Task<ComputeSystemOperationResult>> command)
    {
        _operationKind = OperationKind.ExtensionTask;
        Name = name;
        IconGlyph = icon;
        ExtensionTask = command;
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
        // We'll need to disable the card UI while the operation is in progress and handle failures.
        Task.Run(async () =>
        {
            if (_operationKind == OperationKind.DevHomeAction)
            {
                DevHomeAction!();
                return;
            }

            // We'll need to handle the case where the DevHome service is not available.
            await ExtensionTask!(string.Empty);
        });
    }
}
