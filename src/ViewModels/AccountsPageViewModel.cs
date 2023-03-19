// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;

namespace DevHome.ViewModels;

public class AccountsPageViewModel
{
    public ObservableCollection<AccountsProviderViewModel> AccountsProviders { get; } = new ();

    public AccountsPageViewModel()
    {
        var devIdProviders = Application.Current.GetService<IAccountsService>().GetDevIdProviders();
        devIdProviders.ToList().ForEach((devIdProvider) =>
        {
            AccountsProviders.Add(new AccountsProviderViewModel(devIdProvider));
        });
    }
}
