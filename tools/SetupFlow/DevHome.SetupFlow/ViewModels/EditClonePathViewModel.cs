// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View model to handle the dialog when users want to edit the clone path from the repo review page.
/// </summary>
public partial class EditClonePathViewModel : ObservableObject
{
    /// <summary>
    /// Controls if the error text should be shown.
    /// </summary>
    [ObservableProperty]
    private Visibility _showErrorTextBox;

    /// <summary>
    /// Controls if the primary button should be enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isPrimaryButtonEnabled;

    /// <summary>
    /// Controls if the warning message for removing the new dev drive should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowAreYouSureMessage;

    public EditClonePathViewModel()
    {
        ShowErrorTextBox = Visibility.Collapsed;
        IsPrimaryButtonEnabled = false;
    }
}
