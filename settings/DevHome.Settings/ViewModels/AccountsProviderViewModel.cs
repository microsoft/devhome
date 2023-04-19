// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Settings.Models;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    public IDeveloperIdProvider DeveloperIdProvider { get; }

    public string ProviderName => DeveloperIdProvider.GetName();

    public ObservableCollection<Account> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDeveloperIdProvider devIdProvider)
    {
        DeveloperIdProvider = devIdProvider;
        RefreshLoggedInAccounts();
    }

    public void RefreshLoggedInAccounts()
    {
        LoggedInAccounts.Clear();
        DeveloperIdProvider.GetLoggedInDeveloperIds().ToList().ForEach((devId) =>
        {
            LoggedInAccounts.Add(new Account(this, devId));
        });
    }

    public void RemoveAccount(string loginId)
    {
        var accountToRemove = LoggedInAccounts.FirstOrDefault(x => x.LoginId == loginId);
        if (accountToRemove != null)
        {
            try
            {
                DeveloperIdProvider.LogoutDeveloperId(accountToRemove.GetDevId());
            }
            catch (Exception ex)
            {
                LoggerFactory.Get<ILogger>().Log($"RemoveAccount() failed", LogLevel.Local, $"developerId: {loginId} Error: {ex}");
                throw;
            }
        }

        RefreshLoggedInAccounts();
    }
}
