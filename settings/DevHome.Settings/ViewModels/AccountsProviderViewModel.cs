// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using DevHome.Telemetry;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    private IPluginService _pluginService;

    public IDeveloperIdProvider DeveloperIdProvider { get; }

    public string ProviderName => DeveloperIdProvider.GetName();

    public ObservableCollection<Account> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDeveloperIdProvider devIdProvider, IPluginService pluginService)
    {
        DeveloperIdProvider = devIdProvider;
        RefreshLoggedInAccounts();
        _pluginService = pluginService;
    }

    public void RefreshLoggedInAccounts()
    {
        LoggedInAccounts.Clear();
        var result = _pluginService.RunQueryAsync(() => DeveloperIdProvider.GetLoggedInDeveloperIds().Select((devId) => new Account(this, devId.LoginId())));
        if (result.IsSuccessful)
        {
            foreach (var account in result.ResultData!)
            {
                LoggedInAccounts.Add(account);
            }
        }
        else
        {
            // TODO: Display Error
            LoggedInAccounts.Clear();
        }
    }

    public void RemoveAccount(string loginId)
    {
        var result = _pluginService.RunQueryAsync(() =>
        {
            var devIdToLogout = DeveloperIdProvider.GetLoggedInDeveloperIds().Where(devId => devId.LoginId() == loginId).FirstOrDefault();
            if (devIdToLogout != null)
            {
                DeveloperIdProvider.LogoutDeveloperId(devIdToLogout);
                return true;
            }

            return false;
        });

        if (!result.IsSuccessful)
        {
            // TODO Display Error
            Log.Logger?.ReportError($"developerId: {loginId} Error: {result.Exception}", result.Exception!);
        }

        RefreshLoggedInAccounts();
    }
}
