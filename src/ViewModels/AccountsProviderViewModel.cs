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

    public async Task ShowLoginUI(string loginEntryPoint, Page page)
    {
        string[] args = { loginEntryPoint };
        var loginUIAdaptiveCardController = _devIdProvider.GetAdaptiveCardController(args);
        var pluginAdaptiveCardPanel = new PluginAdaptiveCardPanel();
        pluginAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, AdaptiveCardRendererHelper.GetLoginUIRenderer());
        pluginAdaptiveCardPanel.RequestedTheme = page.ActualTheme;

        var loginUIContentDialog = new ContentDialog
        {
            Content = pluginAdaptiveCardPanel,
            XamlRoot = page.XamlRoot,
            RequestedTheme = page.ActualTheme,
            CloseButtonText = "X",
            CloseButtonStyle = (Style)page.Resources["CloseButton"],
            MinWidth = 350,
            MaxWidth = 350,
            MinHeight = 900,
            MaxHeight = 900,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        await loginUIContentDialog.ShowAsync();
        RefreshLoggedInAccounts();

        // TODO: Await Login event to match up the loginEntryPoint and return DeveloperId
        loginUIAdaptiveCardController.Dispose();
    }

    public void RefreshLoggedInAccounts()
    {
        LoggedInAccounts.Clear();
        _accountsService.GetDeveloperIds(_devIdProvider).ToList().ForEach((devId) =>
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
    }
}
