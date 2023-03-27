// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Labs.WinUI;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
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
            var confirmLogoutContentDialog = new ContentDialog
            {
                Title = "No Dev Home Plugins found!",
                Content = "Please install a Dev Home Plugin and restart Dev Home to add an account.",
                PrimaryButtonText = "Ok",
                XamlRoot = XamlRoot,
            };

            await confirmLogoutContentDialog.ShowAsync();
            return;
        }

        // TODO: expand this for multiple providers after their buttons are added
        ViewModel.AccountsProviders.First().AddAccount();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var confirmLogoutContentDialog = new ContentDialog
        {
            Title = "Are you sure?",
            Content = "Are you sure you want to remove this user account?"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "Dev Home will no longer be able to access online resources that use this account.",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No",
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

        ViewModel.AccountsProviders.First().RemoveAccount(loginIdToRemove);

        var afterLogoutContentDialog = new ContentDialog
        {
            Title = "Logout Successful",
            Content = loginIdToRemove + " has successfully logged out",
            PrimaryButtonText = "OK",
            XamlRoot = XamlRoot,
        };
        _ = await afterLogoutContentDialog.ShowAsync();
    }
}
