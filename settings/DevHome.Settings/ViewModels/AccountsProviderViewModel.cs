// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Views;
using DevHome.Settings.Helpers;
using DevHome.Settings.Models;
using DevHome.Settings.Views;
using DevHome.Telemetry;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Settings.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    private readonly IDeveloperIdProvider _devIdProvider;

    public ObservableCollection<Account> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDeveloperIdProvider devIdProvider)
    {
        _devIdProvider = devIdProvider;
        RefreshLoggedInAccounts();
    }

    public string ProviderName => _devIdProvider.GetName();

    public async Task ShowLoginUIAsync(string loginEntryPoint, Page parentPage)
    {
        string[] args = { loginEntryPoint };
        var loginUIAdaptiveCardController = _devIdProvider.GetAdaptiveCardController(args);
        var pluginAdaptiveCardPanel = new PluginAdaptiveCardPanel();
        pluginAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, AdaptiveCardRendererHelper.GetLoginUIRenderer());
        pluginAdaptiveCardPanel.RequestedTheme = parentPage.ActualTheme;

        // TODO: Replace Close button with "X"
        var loginUIContentDialog = new LoginUIDialog
        {
            Content = pluginAdaptiveCardPanel,
            XamlRoot = parentPage.XamlRoot,
            RequestedTheme = parentPage.ActualTheme,
            CloseButtonText = "Close",
        };
        await loginUIContentDialog.ShowAsync();
        RefreshLoggedInAccounts();

        // TODO: Await Login event to match up the loginEntryPoint and return DeveloperId
        loginUIAdaptiveCardController.Dispose();
    }

    public void RefreshLoggedInAccounts()
    {
        LoggedInAccounts.Clear();
        _devIdProvider.GetLoggedInDeveloperIds().ToList().ForEach((devId) =>
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
