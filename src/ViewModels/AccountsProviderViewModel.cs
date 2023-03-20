// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Antlr4.Runtime.Misc;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    private readonly IDevIdProvider _devIdProvider;
    private readonly IAccountsService _accountsService;

    public ObservableCollection<AccountViewModel> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDevIdProvider devIdProvider)
    {
        _devIdProvider = devIdProvider;
        _accountsService = Application.Current.GetService<IAccountsService>();
        RefreshLoggedInAccounts();
    }

    public string ProviderName => _devIdProvider.GetName();

    public void RefreshLoggedInAccounts() =>
        _accountsService.GetDeveloperIds(_devIdProvider).ToList().ForEach((devId) =>
        {
            LoggedInAccounts.Add(new AccountViewModel(this, devId));
        });

    public PluginAdaptiveCardPanel GetLoginUI()
    {
        string[] args = { "Settings" };
        var loginUIAdaptiveCardController = _devIdProvider.GetAdaptiveCardController(args);
        var pluginAdaptiveCardPanel = new PluginAdaptiveCardPanel();
        pluginAdaptiveCardPanel.Bind(loginUIAdaptiveCardController);
        return pluginAdaptiveCardPanel;
    }

    public void RemoveAccount(string loginId)
    {
        var accountToRemove = LoggedInAccounts.FirstOrDefault(x => x.LoginId == loginId);
        if (accountToRemove != null)
        {
            try
            {
                _devIdProvider.LogoutDeveloperId(accountToRemove.GetDevId());
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
