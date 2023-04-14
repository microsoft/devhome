// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI.Controls;
using DevHome.Common.Views;
using DevHome.Settings.Helpers;
using DevHome.Settings.Models;
using DevHome.Settings.Views;
using DevHome.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    public IDeveloperIdProvider DevIdProvider { get; }

    public ObservableCollection<Account> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDeveloperIdProvider devIdProvider)
    {
        DevIdProvider = devIdProvider;
        RefreshLoggedInAccounts();
    }

    public string ProviderName => DevIdProvider.GetName();

    public void RefreshLoggedInAccounts()
    {
        LoggedInAccounts.Clear();
        DevIdProvider.GetLoggedInDeveloperIds().ToList().ForEach((devId) =>
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
                DevIdProvider.LogoutDeveloperId(accountToRemove.GetDevId());
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
