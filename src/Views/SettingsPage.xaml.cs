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

        // TODO: expand this for multiple providers after their buttons are added
        AccountsPageViewModel.AccountsProviders.First().AddAccount();
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

        var loginIdToRemove = (sender as Button)?.Tag.ToString();
        if (string.IsNullOrEmpty(loginIdToRemove))
        {
            return;
        }

        AccountsPageViewModel.AccountsProviders.First().RemoveAccount(loginIdToRemove);

        var afterLogoutContentDialog = new ContentDialog
        {
            Title = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Title"),
            Content = loginIdToRemove + resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Content"),
            PrimaryButtonText = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_PrimaryButtonText"),
            XamlRoot = XamlRoot,
        };
        _ = await afterLogoutContentDialog.ShowAsync();
    }
}
