// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.ViewModels;

public partial class SearchMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _primaryMessage;

    [ObservableProperty]
    private string _secondaryMessage;
}
