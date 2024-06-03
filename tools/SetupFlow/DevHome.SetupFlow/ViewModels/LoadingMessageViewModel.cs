// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View Model to hold information for each message in the loading screen.
/// </summary>
public partial class LoadingMessageViewModel : ObservableObject
{
    /// <summary>
    /// Gets the message to display in the loading screen.
    /// </summary>
    [ObservableProperty]
    private string _messageToShow;

    /// <summary>
    /// The icon to display in the loading screen after a task is finished.
    /// </summary>
    [ObservableProperty]
    private BitmapImage _statusSymbolIcon;

    // This and TextTrimmed() are used to enable the UI to show a tooltip
    [ObservableProperty]
    private bool _isRepoNameTrimmed;

    [RelayCommand]
    public void TextTrimmed()
    {
        IsRepoNameTrimmed = true;
    }
}
