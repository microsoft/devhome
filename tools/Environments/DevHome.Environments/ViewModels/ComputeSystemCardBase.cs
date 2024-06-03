// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;
using Windows.Foundation;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// Base class for all compute system cards that will appear in the UI.
/// </summary>
public abstract partial class ComputeSystemCardBase : ObservableObject
{
    public string Name { get; protected set; } = string.Empty;

    public string AlternativeName { get; protected set; } = string.Empty;

    public DateTime LastConnected { get; protected set; } = DateTime.Now;

    public bool IsCardCreating { get; protected set; }

    public event TypedEventHandler<ComputeSystemCardBase, string>? ComputeSystemErrorReceived;

    // Will hold the supported actions that the user can perform on in the UI. E.g Remove button
    public ObservableCollection<OperationsViewModel> DotOperations { get; protected set; } = new();

    [ObservableProperty]
    private ComputeSystemState _state;

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private CardStateColor _stateColor;

    [ObservableProperty]
    private bool _shouldShowLaunchOperation;

    /// <summary>
    /// This string can, and should, be in a resource file.  But can't because this string used in a
    /// data template.
    /// </summary>
    [ObservableProperty]
    private string _moreOptionsButtonName;

    public BitmapImage? HeaderImage { get; protected set; }

    public BitmapImage? BodyImage { get; protected set; }

    public string ProviderDisplayName { get; protected set; } = string.Empty;

    public string AssociatedProviderId { get; protected set; } = string.Empty;

    public string ComputeSystemId { get; protected set; } = string.Empty;

    [ObservableProperty]
    private string _uiMessageToDisplay = string.Empty;

    public ComputeSystemCardBase()
    {
        var stringResource = new StringResource("DevHome.Environments.pri", "DevHome.Environments/Resources");
        _moreOptionsButtonName = stringResource.GetLocalized("MoreOptionsButtonName");
    }

    public override string ToString()
    {
        return $"{Name} {AlternativeName}";
    }

    /// <summary>
    /// Common way to send error message to UI for classes that implement ComputeSystemCardBase
    /// </summary>
    /// <param name="errorMessage">Error message to send to the user</param>
    public virtual void OnErrorReceived(string errorMessage)
    {
        ComputeSystemErrorReceived?.Invoke(this, errorMessage);
    }
}
