// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// ViewModel for displaying errors on the summary page.
/// </summary>
public partial class SummaryErrorMessageViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the message to display in the error section of the summary screen.
    /// </summary>
    public string MessageToShow
    {
        get; set;
    }

    /// <summary>
    /// The icon to display.
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
