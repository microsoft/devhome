// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.Models;
public partial class TaskInformation : ObservableObject
{
    public int TaskIndex
    {
        get; set;
    }

    public ISetupTask TaskToExecute
    {
        get; set;
    }

    /// <summary>
    /// The message to display in the loading screen.
    /// </summary>
    [ObservableProperty]
    private string _messageToShow;

    /// <summary>
    /// If the status icon, green, yellow, or red, should be shown.
    /// </summary>
    [ObservableProperty]
    private bool _statusIconGridVisibility;

    /// <summary>
    /// If the progress ring should be shown.  Only show a progress ring when the task is running.
    /// </summary>
    [ObservableProperty]
    private bool _shouldShowProgressRing;

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
}
