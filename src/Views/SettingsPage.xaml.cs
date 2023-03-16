// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public AccountsPageViewModel AccountsViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = Application.Current.GetService<SettingsViewModel>();
        AccountsViewModel = Application.Current.GetService<AccountsPageViewModel>();
        InitializeComponent();
    }

    private void AddDeveloperId_Click(object sender, RoutedEventArgs e)
    {
        AccountsViewModel.AddAccount();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var confirmLogoutContentDialog = new ContentDialog()
        {
            Title = "Are you sure?",
            Content = "Are you sure you want to remove this user account?"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Dev Home will no longer be able to access online resources that use this account.",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
        };
        confirmLogoutContentDialog.XamlRoot = this.XamlRoot;
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

        AccountsViewModel.RemoveAccount(loginIdToRemove);

        var afterLogoutContentDialog = new ContentDialog()
        {
            Title = "Logout Successful",
            Content = loginIdToRemove + " has successfully logged out",
            PrimaryButtonText = "OK",
        };

        afterLogoutContentDialog.XamlRoot = this.XamlRoot;
        _ = await afterLogoutContentDialog.ShowAsync();
    }
}
