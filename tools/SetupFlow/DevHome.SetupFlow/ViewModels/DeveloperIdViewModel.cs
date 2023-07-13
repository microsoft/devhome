// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.ViewModels;
public partial class DeveloperIdViewModel : ObservableObject
{
    public IDeveloperId DeveloperId { get; }

    [ObservableProperty]
    private string _loginId;

    public DeveloperIdViewModel(IDeveloperId developerId)
    {
        DeveloperId = developerId;
        LoginId = developerId.LoginId();
    }
}
