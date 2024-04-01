// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// Represents an operation that can be performed on a compute system.
/// This is used to populate the launch and dot buttons on the compute system card.
/// </summary>
public partial class OperationsViewModel
{
    public string Name { get; }

    public string? Description { get; set; }

    public string IconGlyph { get; }

    private Func<string, Task<ComputeSystemOperationResult>>? CommandWithString { get; }

    private Func<bool, Task<ComputeSystemOperationResult>>? CommandWithBool { get; }

    private readonly bool commandBool;

    public OperationsViewModel(string name, string icon, Func<string, Task<ComputeSystemOperationResult>> command)
    {
        Name = name;
        IconGlyph = icon;
        CommandWithString = command;
    }

    public OperationsViewModel(string name, string icon, Func<bool, Task<ComputeSystemOperationResult>> command, bool value)
    {
        Name = name;
        IconGlyph = icon;
        CommandWithBool = command;
        commandBool = value;
    }

    [RelayCommand]
    public void InvokeAction()
    {
        // We'll need to disable the card UI while the operation is in progress and handle failures.
        Task.Run(async () =>
        {
            if (CommandWithString != null)
            {
                await CommandWithString(string.Empty);
            }
            else if (CommandWithBool != null)
            {
                await CommandWithBool(commandBool);
            }
        });
    }
}
