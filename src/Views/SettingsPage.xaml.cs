// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Services;
using DevHome.Telemetry;
using DevHome.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public AccountsPageViewModel AccountsPageViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = Application.Current.GetService<SettingsViewModel>();
        AccountsPageViewModel = Application.Current.GetService<AccountsPageViewModel>();
        InitializeComponent();
    }

    private async void AddDeveloperId_Click(object sender, RoutedEventArgs e)
    {
        if (AccountsPageViewModel.AccountsProviders.Count == 0)
        {
            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
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

        // TODO: expand this for multiple providers after their buttons are added
        if (sender as Button is Button addAccountButton)
        {
            if (addAccountButton.Tag is AccountsProviderViewModel accountProvider)
            {
                try
                {
                    await accountProvider.ShowLoginUIAsync("Settings", this);
                }
                catch (Exception ex)
                {
                    LoggerFactory.Get<ILogger>().Log($"AddAccount_Click(): loginUIContentDialog failed", LogLevel.Local, $"Error: {ex} Sender: {sender} RoutedEventArgs: {e}");
                }

                accountProvider.RefreshLoggedInAccounts();
            }
            else
            {
                LoggerFactory.Get<ILogger>().Log($"AddAccount_Click(): addAccountButton.Tag is not AccountsProviderViewModel", LogLevel.Local, $"Sender: {sender} RoutedEventArgs: {e}");
                return;
            }
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
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
        if (sender is Button { Tag: AccountViewModel accountToRemove })
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
