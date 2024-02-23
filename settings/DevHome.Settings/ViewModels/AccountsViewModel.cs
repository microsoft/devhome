// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public class AccountsViewModel : BreadcrumbViewModel
{
    public ObservableCollection<AccountsProviderViewModel> AccountsProviders { get; } = new();

    public override ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AccountsViewModel()
    {
        var devIdProviders = Task.Run(async () => await Application.Current.GetService<IAccountsService>().GetDevIdProviders()).Result.ToList();
        devIdProviders.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.OrdinalIgnoreCase));
        devIdProviders.ForEach((devIdProvider) =>
        {
            AccountsProviders.Add(new AccountsProviderViewModel(devIdProvider));
        });

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_Accounts_Header"), typeof(AccountsViewModel).FullName!),
        };
    }
}
