// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

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

    private Func<string, IAsyncOperation<ComputeSystemOperationResult>> Command { get; }

    public OperationsViewModel(string name, string icon, Func<string, IAsyncOperation<ComputeSystemOperationResult>> command)
    {
        Name = name;
        IconGlyph = icon;
        Command = command;
    }

    [RelayCommand]
    public void InvokeAction()
    {
        Command(string.Empty).GetAwaiter().GetResult();
    }
}
