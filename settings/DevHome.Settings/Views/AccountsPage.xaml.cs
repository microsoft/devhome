// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
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

    public AccountsPage()
    {
        ViewModel = Application.Current.GetService<AccountsViewModel>();
        this.InitializeComponent();
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
