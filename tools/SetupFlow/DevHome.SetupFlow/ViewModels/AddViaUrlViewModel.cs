// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.ViewModels;
public partial class AddViaUrlViewModel : ObservableObject
{
    [ObservableProperty]
    private string _uri;

    [ObservableProperty]
    private string _uriError;

    [ObservableProperty]
    private bool _shouldShowUriError;
}
