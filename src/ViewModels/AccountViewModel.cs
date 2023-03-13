// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    private readonly IDeveloperId _devId;

    internal IDeveloperId GetDevId() => _devId;

    public AccountViewModel(IDeveloperId devId)
    {
        _devId = devId;
    }

    public string LoginId => _devId.LoginId();
}
