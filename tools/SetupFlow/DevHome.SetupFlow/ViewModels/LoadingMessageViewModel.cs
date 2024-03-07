// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Views;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// View Model to hold information for each message in the loading screen.
/// </summary>
public partial class LoadingMessageViewModel : ObservableObject
{
    /// <summary>
    /// Gets the message to display in the loading screen.
    /// </summary>
    public string MessageToShow { get; }

    /// <summary>
    /// If the progress ring should be shown.  Only show a progress ring when the task is running.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowProgressRing;

    /// <summary>
    /// The status symbol icon is the red, green, or yellow icon that is next to a task when it has been completed.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowStatusSymbolIcon;

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

    public LoadingMessageViewModel(string messageToShow)
    {
        MessageToShow = messageToShow;
    }
}
