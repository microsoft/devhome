// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using DevHome.Common.Extensions;
using DevHome.Telemetry;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

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
                try
                {
                    _loginUIContentDialog.Content = accountProvider.GetLoginUI();
                    _loginUIContentDialog.XamlRoot = this.Content.XamlRoot;
                    _ = await _loginUIContentDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    LoggerFactory.Get<ILogger>().Log($"AddAccount_Click(): _loginUIContentDialog failed", LogLevel.Local, $"Error: {ex} Sender: {sender} RoutedEventArgs: {e}");
                }

                accountProvider.RefreshLoggedInAccounts();

                // Bring focus back to DevHome after login
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
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
        // Confirmation Content Dialog
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
                Content = accountToRemove.LoginId + resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_Content"),
                CloseButtonText = resourceLoader.GetString("Settings_Accounts_AfterLogoutContentDialog_PrimaryButtonText"),
                XamlRoot = XamlRoot,
            };
            _ = await afterLogoutContentDialog.ShowAsync();
        }
    }
}
