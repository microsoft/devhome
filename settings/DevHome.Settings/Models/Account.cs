// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Settings.ViewModels;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.Models;

public partial class Account : ObservableObject
{
    private readonly AccountsProviderViewModel _accountsProvider;

    public Account(AccountsProviderViewModel accountsProvider, string loginId)
    {
        _accountsProvider = accountsProvider;
        LoginId = loginId;
    }

    public string LoginId { get; }

    public void RemoveAccount() => _accountsProvider.RemoveAccount(LoginId);
}
