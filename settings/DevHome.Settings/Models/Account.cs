// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Settings.ViewModels;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.Models;

public partial class Account : ObservableObject
{
    private readonly IDeveloperId _devId;

    private readonly AccountsProviderViewModel _accountsProvider;

    internal IDeveloperId GetDevId() => _devId;

    public Account(AccountsProviderViewModel accountsProvider, IDeveloperId devId)
    {
        _accountsProvider = accountsProvider;
        _devId = devId;
    }

    public string LoginId => _devId.LoginId;

    public string ProviderName => _accountsProvider.ProviderName;

    public void RemoveAccount() => _accountsProvider.RemoveAccount(_devId.LoginId);
}
