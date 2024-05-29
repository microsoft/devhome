// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Services;
using DevHome.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

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
public partial class OperationsViewModel : IEquatable<OperationsViewModel>
{
    private readonly Guid _operationId = Guid.NewGuid();

    private readonly OperationKind _operationKind;

    private readonly string _additionalContext = string.Empty;

    public string Name { get; }

    public ComputeSystemOperations ComputeSystemOperation { get; }

    public string IconGlyph { get; }

    private Func<string, Task<ComputeSystemOperationResult>>? ExtensionTask { get; }

    private Action? DevHomeAction { get; }

    private readonly Window? _mainWindow;

    private readonly StringResource _stringResource = new("DevHome.Environments.pri", "DevHome.Environments/Resources");

    public OperationsViewModel(
        string name,
        string icon,
        Func<string, Task<ComputeSystemOperationResult>> command,
        ComputeSystemOperations computeSystemOperation,
        Window? mainWindow = null)
    {
        _operationKind = OperationKind.ExtensionTask;
        Name = name;
        IconGlyph = icon;
        ExtensionTask = command;
        ComputeSystemOperation = computeSystemOperation;
        _mainWindow = mainWindow;
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
    }

    private void RunAction()
    {
        // To Do: Need to disable the card UI while the operation is in progress and handle failures.
        Task.Run(async () =>
        {
            if (_operationKind == OperationKind.DevHomeAction)
            {
                DevHomeAction!();
                return;
            }

            var activityId = Guid.NewGuid();
            WeakReferenceMessenger.Default.Send(new ComputeSystemOperationStartedMessage(new(ComputeSystemOperation, _additionalContext, activityId)), this);

            var result = await ExtensionTask!(string.Empty);

            WeakReferenceMessenger.Default.Send(new ComputeSystemOperationCompletedMessage(new(ComputeSystemOperation, result, _additionalContext, activityId)), this);
        });
    }

    [RelayCommand]
    public void InvokeAction()
    {
        // Show confirmation popup in case of delete
        if (ComputeSystemOperation == ComputeSystemOperations.Delete)
        {
            ContentDialog noWifiDialog = new ContentDialog
            {
                Title = _stringResource.GetLocalized("DeleteEnviroment_Title"),
                Content = _stringResource.GetLocalized("DeleteEnviroment_Content"),
                PrimaryButtonText = _stringResource.GetLocalized("DeleteEnviroment_ConfirmButton"),
                SecondaryButtonText = _stringResource.GetLocalized("DeleteEnviroment_CancelButton"),
                XamlRoot = _mainWindow?.Content.XamlRoot,
            };

            _mainWindow?.DispatcherQueue?.TryEnqueue(async () =>
            {
                var result = await noWifiDialog.ShowAsync();

                // Delete the enviroment after confirmation
                if (result == ContentDialogResult.Primary)
                {
                    RunAction();
                }
            });
        }
        else
        {
            RunAction();
        }
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
