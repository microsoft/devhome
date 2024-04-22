// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Settings.Models;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.Settings.ViewModels;

public partial class AccountsProviderViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(AccountsProviderViewModel));

    public IDeveloperIdProvider DeveloperIdProvider { get; }

    public string ProviderName => DeveloperIdProvider.DisplayName;

    public ObservableCollection<Account> LoggedInAccounts { get; } = new();

    public AccountsProviderViewModel(IDeveloperIdProvider devIdProvider)
    {
        DeveloperIdProvider = devIdProvider;
        RefreshLoggedInAccounts();
    }

    public void RefreshLoggedInAccounts()
    {
        LoggedInAccounts.Clear();
        var developerIdsResult = DeveloperIdProvider.GetLoggedInDeveloperIds();
        if (developerIdsResult.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"{developerIdsResult.Result.DisplayMessage} - {developerIdsResult.Result.DiagnosticText}");
            return;
        }

        developerIdsResult.DeveloperIds.ToList().ForEach((devId) =>
        {
            LoggedInAccounts.Add(new Account(this, devId));
        });
    }

    public void RemoveAccount(string loginId)
    {
        var accountToRemove = LoggedInAccounts.FirstOrDefault(x => x.LoginId == loginId);
        if (accountToRemove != null)
        {
            var providerOperationResult = DeveloperIdProvider.LogoutDeveloperId(accountToRemove.GetDevId());
            if (providerOperationResult.Status == ProviderOperationStatus.Failure)
            {
                _log.Error($"{providerOperationResult.DisplayMessage} - {providerOperationResult.DiagnosticText}");
                return;
            }
        }

        RefreshLoggedInAccounts();
    }
}
