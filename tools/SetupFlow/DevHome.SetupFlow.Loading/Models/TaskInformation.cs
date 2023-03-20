// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.SetupFlow.Common.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DevHome.SetupFlow.Loading.Models;
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

    [ObservableProperty]
    private string _messageToShow;

    [ObservableProperty]
    private Visibility _statusIconGridVisibility;

    [ObservableProperty]
    private Brush _circleForeground;

    [ObservableProperty]
    private string _statusSymbolHex;
}
