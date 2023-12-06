// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.ViewModels;

public partial class UserAccountViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private bool _enabled;
}
