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

        var loginUIContentDialog = new LoginUIDialog();

        Button cancelButton = new Button();
        cancelButton.Content = "x";
        cancelButton.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Right;
        cancelButton.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top;
        cancelButton.Background = new SolidColorBrush(Colors.Transparent);
        cancelButton.Click += (sender, args) =>
        {
            loginUIContentDialog.Hide();
        };

        var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
        TextBlock title = new TextBlock();
        title.Text = resourceLoader.GetString("AccountLoginUI_DialogTitle_Text");
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.VerticalAlignment = VerticalAlignment.Top;
        title.FontSize = 22;
        title.FontWeight = FontWeights.Bold;

        var heading = new DockPanel();
        heading.Children.Add(title);
        heading.Children.Add(cancelButton);
        DockPanel.SetDock(cancelButton, Dock.Right);
        DockPanel.SetDock(title, Dock.Left);

        var uiControlObject = new StackPanel();
        uiControlObject.Children.Add(heading);
        uiControlObject.Children.Add(pluginAdaptiveCardPanel);

        loginUIContentDialog.Content = uiControlObject;
        loginUIContentDialog.XamlRoot = parentPage.XamlRoot;
        loginUIContentDialog.RequestedTheme = parentPage.ActualTheme;

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
