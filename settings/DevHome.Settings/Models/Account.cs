// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.Models;

public partial class Account : ObservableObject
{
    private readonly IDeveloperId _devId;

    private readonly AccountsProviderViewModel _accountsProvider;

    internal IDeveloperId GetDevId() => _devId;

    public Account(IDeveloperId devId)
    {
        _devId = devId;
        _accountsProvider = accountsProviderViewModel;
    }

    public string LoginId => _devId.LoginId();

    public void RemoveAccount() => _accountsProvider.RemoveAccount(_devId.LoginId());
}
