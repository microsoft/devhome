// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Configuration.Provider;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Environments.Models;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Environments.ViewModels;

<<<<<<< Updated upstream
=======
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

>>>>>>> Stashed changes
/// <summary>
/// Represents an operation that can be performed on a compute system.
/// This is used to populate the launch and dot buttons on the compute system card.
/// </summary>
public partial class OperationsViewModel : IEquatable<OperationsViewModel>
{
<<<<<<< Updated upstream
=======
    private readonly Guid _operationId = Guid.NewGuid();

    private readonly OperationKind _operationKind;

    private readonly string _additionalContext = string.Empty;

>>>>>>> Stashed changes
    public string Name { get; }

    public ComputeSystemOperations ComputeSystemOperation { get; }

    public string IconGlyph { get; }

    private Func<string, Task<ComputeSystemOperationResult>> Command { get; }

    public OperationsViewModel(string name, string icon, Func<string, Task<ComputeSystemOperationResult>> command, ComputeSystemOperations computeSystemOperation)
    {
        Name = name;
        IconGlyph = icon;
<<<<<<< Updated upstream
        Command = command;
=======
        ExtensionTask = command;
        ComputeSystemOperation = computeSystemOperation;
    }

    public OperationsViewModel(
        string name,
        string icon,
        Func<string, Task<ComputeSystemOperationResult>> command,
        ComputeSystemOperations computeSystemOperation,
        string additionalContext)
    {
        _operationKind = OperationKind.ExtensionTask;
        Name = name;
        IconGlyph = icon;
        ExtensionTask = command;
        ComputeSystemOperation = computeSystemOperation;
        _additionalContext = additionalContext;
    }

    public OperationsViewModel(string name, string icon, Action command)
    {
        _operationKind = OperationKind.DevHomeAction;
        Name = name;
        IconGlyph = icon;
        DevHomeAction = command;
>>>>>>> Stashed changes
    }

    [RelayCommand]
    public void InvokeAction()
    {
        Task.Run(async () =>
        {
<<<<<<< Updated upstream
            await Command(string.Empty);
=======
            if (_operationKind == OperationKind.DevHomeAction)
            {
                DevHomeAction!();
                return;
            }

            var activityId = Guid.NewGuid();
            WeakReferenceMessenger.Default.Send(new ComputeSystemOperationStartedMessage(new(ComputeSystemOperation, _additionalContext, activityId)), this);

            var result = await ExtensionTask!(string.Empty);

            WeakReferenceMessenger.Default.Send(new ComputeSystemOperationCompletedMessage(new(ComputeSystemOperation, result, _additionalContext, activityId)), this);
>>>>>>> Stashed changes
        });
    }

    /// <summary>
    /// Compares the current instance of the object with another instance of the object to check if they are the same.
    /// </summary>
    /// <param name="other">A OperationsViewModel to compare the current object with</param>
    /// <returns>True is the object in parameter is equal to the current object</returns>
    public bool Equals(OperationsViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (ReferenceEquals(null, other))
        {
            return false;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as OperationsViewModel);
    }

    public override int GetHashCode()
    {
        return $"{Name}#{_operationKind}#{IconGlyph}#{_operationId}".GetHashCode();
    }
}
