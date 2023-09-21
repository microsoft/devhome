// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.ViewModels;
public partial class SummaryMessageViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the message to display in the loading screen.
    /// </summary>
    public string MessageToShow
    {
        get; set;
    }

    /// <summary>
    /// The icon to display in the loading screen after a task is finished.
    /// </summary>
    [ObservableProperty]
    private BitmapImage _statusSymbolIcon;

    // This and TextTrimmed() are used to enable the UI to show a tooltip
    [ObservableProperty]
    private bool _isMessageTrimmed;

    [RelayCommand]
    public void TextTrimmed()
    {
        IsMessageTrimmed = true;
    }
}
