// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public class AccountsViewModel : ObservableRecipient
{
    public ObservableCollection<AccountsProviderViewModel> AccountsProviders { get; } = new ();

    public AccountsViewModel()
    {
        var devIdProviders = Application.Current.GetService<IAccountsService>().GetDevIdProviders();
        devIdProviders.ToList().ForEach((devIdProvider) =>
        {
            AccountsProviders.Add(new AccountsProviderViewModel(devIdProvider));
        });
    }
}
