// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public class AccountsViewModel : ObservableObject
{
    public ObservableCollection<AccountsProviderViewModel> AccountsProviders { get; } = new ();

    public AccountsViewModel()
    {
        var devIdProviders = Task.Run(async () => await Application.Current.GetService<IAccountsService>().GetDevIdProviders()).Result;
        devIdProviders.ToList().ForEach((devIdProvider) =>
        {
            AccountsProviders.Add(new AccountsProviderViewModel(devIdProvider));
        });
    }
}
