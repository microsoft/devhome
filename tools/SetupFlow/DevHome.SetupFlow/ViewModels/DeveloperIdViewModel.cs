// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;
public partial class DeveloperIdViewModel : ObservableObject
{
    private readonly IDeveloperId _developerId;

    [ObservableProperty]
    private string _loginId;

    public DeveloperIdViewModel(IDeveloperId developerId)
    {
        _developerId = developerId;
        LoginId = developerId.LoginId();
    }
}
