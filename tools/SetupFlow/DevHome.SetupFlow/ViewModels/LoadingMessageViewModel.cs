// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
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

    /// <summary>
    /// Primary when the task is running, otherwise secondary.
    /// </summary>
    [ObservableProperty]
    private SolidColorBrush _messageForeground;

    public LoadingMessageViewModel(string messageToShow)
    {
        MessageToShow = messageToShow;
    }
}
