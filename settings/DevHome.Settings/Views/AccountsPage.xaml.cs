// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Logging;
using DevHome.Settings.Helpers;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DevHome.Settings.Views;

public sealed partial class AccountsPage : Page
{
    public AccountsViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get;
    }

    public AccountsPage()
    {
        ViewModel = Application.Current.GetService<AccountsViewModel>();
        this.InitializeComponent();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new Breadcrumb(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new Breadcrumb(stringResource.GetLocalized("Settings_Accounts_Header"), typeof(AccountsViewModel).FullName!),
        };
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
    }

    private async void AddDeveloperId_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.AccountsProviders.Count == 0)
        {
            var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "DevHome.Settings/Resources");
            var noProvidersContentDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("Settings_Accounts_NoProvidersContentDialog_Title"),
                Content = resourceLoader.GetString("Settings_Accounts_NoProvidersContentDialog_Content"),
                PrimaryButtonText = resourceLoader.GetString("Settings_Accounts_NoProvidersContentDialog_PrimaryButtonText"),
                XamlRoot = XamlRoot,
            };
            await noProvidersContentDialog.ShowAsync();
            return;
        }

        if (sender as Button is Button addAccountButton)
        {
            if (addAccountButton.Tag is AccountsProviderViewModel accountProvider)
            {
                try
                {
                    await ShowLoginUIAsync("Settings", this, accountProvider);
                }
                catch (Exception ex)
                {
                    GlobalLog.Logger?.ReportError($"AddAccount_Click(): loginUIContentDialog failed - Error: {ex} Sender: {sender} RoutedEventArgs: {e}");
                }

                accountProvider.RefreshLoggedInAccounts();
            }
            else
            {
                GlobalLog.Logger?.ReportInfo($"AddAccount_Click(): addAccountButton.Tag is not AccountsProviderViewModel - Sender: {sender} RoutedEventArgs: {e}");
                return;
            }
        }
    }

    public async Task ShowLoginUIAsync(string loginEntryPoint, Page parentPage, AccountsProviderViewModel accountProvider)
    {
        string[] args = { loginEntryPoint };
        var loginUIAdaptiveCardController = accountProvider.DeveloperIdProvider.GetAdaptiveCardController(args);
        var pluginAdaptiveCardPanel = new PluginAdaptiveCardPanel();
        pluginAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, AdaptiveCardRendererHelper.GetLoginUIRenderer());
        pluginAdaptiveCardPanel.RequestedTheme = parentPage.ActualTheme;

        var loginUIContentDialog = new LoginUIDialog(pluginAdaptiveCardPanel)
        {
            XamlRoot = parentPage.XamlRoot,
            RequestedTheme = parentPage.ActualTheme,
        };

        await loginUIContentDialog.ShowAsync();
        accountProvider.RefreshLoggedInAccounts();

        // TODO: Await Login event to match up the loginEntryPoint and return DeveloperId
        loginUIAdaptiveCardController.Dispose();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var resourceLoader = new ResourceLoader(ResourceLoader.GetDefaultResourceFilePath(), "DevHome.Settings/Resources");
        var confirmLogoutContentDialog = new ContentDialog
        {
            Title = resourceLoader.GetString("Settings_Accounts_ConfirmLogoutContentDialog_Title"),
            Content = resourceLoader.GetString("Settings_Accounts_ConfirmLogoutContentDialog_Content"),
            PrimaryButtonText = resourceLoader.GetString("Settings_Accounts_ConfirmLogoutContentDialog_PrimaryButtonText"),
            SecondaryButtonText = resourceLoader.GetString("Settings_Accounts_ConfirmLogoutContentDialog_SecondaryButtonText"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };
        var contentDialogResult = await confirmLogoutContentDialog.ShowAsync();

        // No action if declined
        if (contentDialogResult.Equals(ContentDialogResult.Secondary))
        {
            return;
        }

        // Remove the account
        if (sender is Button { Tag: Account accountToRemove })
        {
            accountToRemove.RemoveAccount();

            // Confirmation of removal Content Dialog
            var afterLogoutContentDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Title"),
                Content = $"{accountToRemove.LoginId} " + resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Content"),
                CloseButtonText = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_PrimaryButtonText"),
                XamlRoot = XamlRoot,
            };
            _ = await afterLogoutContentDialog.ShowAsync();
        }
    }
}
