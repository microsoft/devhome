// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AdaptiveCards.ObjectModel.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.Helpers;
using DevHome.Telemetry;
using DevHome.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ViewModels;
public partial class AccountsProviderViewModel : ObservableObject
{
    private readonly IDevIdProvider _devIdProvider;

    public ObservableCollection<AccountViewModel> LoggedInAccounts { get; } = new ();

    public AccountsProviderViewModel(IDevIdProvider devIdProvider)
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

        var loginUIContentDialog = new LoginUIDialog
        {
            Content = pluginAdaptiveCardPanel,
            XamlRoot = parentPage.XamlRoot,
            RequestedTheme = parentPage.ActualTheme,
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
            LoggedInAccounts.Add(new AccountViewModel(this, devId));
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
