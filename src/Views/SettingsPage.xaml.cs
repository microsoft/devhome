// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Services;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace DevHome.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    private readonly ContentDialog _loginUIContentDialog;

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
        _loginUIContentDialog = new ContentDialog();
        InitializeComponent();
    }

    private async void AddAccount_Click(object sender, RoutedEventArgs e)
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

        if (sender as Button is Button addAccountButton)
        {
            if (addAccountButton.Tag is AccountsProviderViewModel accountProvider)
            {
                accountProvider.GetLoginUI();

                _loginUIContentDialog.Content = accountProvider.GetLoginUI();
                _loginUIContentDialog.XamlRoot = this.Content.XamlRoot;

                _ = await _loginUIContentDialog.ShowAsync();
            }
            else
            {
                // TODO: Log and handle this case
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

        if (contentDialogResult.Equals(ContentDialogResult.Secondary))
        {
            return;
        }

        if (sender is Button { Tag: AccountViewModel accountToRemove })
        {
            accountToRemove.RemoveAccount();

            var afterLogoutContentDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Title"),
                Content = accountToRemove.LoginId + resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Content"),
                PrimaryButtonText = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_PrimaryButtonText"),
                XamlRoot = XamlRoot,
            };
            _ = await afterLogoutContentDialog.ShowAsync();
        }
    }
}
