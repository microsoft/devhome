// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Dashboard.ViewModels;
using DevHome.Models;
using DevHome.Services;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Windows.System;

namespace DevHome.Views;

public sealed partial class WhatsNewPage : Page
{
    public WhatsNewViewModel ViewModel
    {
        get;
    }

    public WhatsNewPage()
    {
        ViewModel = Application.Current.GetService<WhatsNewViewModel>();
        InitializeComponent();
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Application.Current.GetService<ILocalSettingsService>().SaveSettingAsync(WellKnownSettingsKeys.IsNotFirstRun, true);

        var whatsNewCards = FeaturesContainer.Resources
            .Where((item) => item.Value.GetType() == typeof(WhatsNewCard))
            .Select(card => card.Value as WhatsNewCard)
            .OrderBy(card => card?.Priority ?? 0);

        foreach (var card in whatsNewCards)
        {
            if (card is null)
            {
                continue;
            }

            ViewModel.AddCard(card);
        }
    }

    private void ConnectToAccountsButton_Click(object sender, RoutedEventArgs e)
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(AccountsViewModel).FullName!);
    }

    private async void Button_ClickAsync(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;

        if (btn?.DataContext is not string pageKey)
        {
            return;
        }

        if (pageKey.StartsWith("ms-settings", StringComparison.InvariantCultureIgnoreCase))
        {
            _ = await Launcher.LaunchUriAsync(new Uri("ms-settings:disksandvolumes"));
        }
        else
        {
            var navigationService = Application.Current.GetService<INavigationService>();
            navigationService.NavigateTo(pageKey!);
        }
    }

    public static class MyHelpers
    {
        public static Type GetType(object ele)
        {
            return ele.GetType();
        }
    }
}
