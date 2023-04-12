// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Models;
using DevHome.Services;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using DevHome.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

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
            .Select(card => card.Value as WhatsNewCard);

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
}
