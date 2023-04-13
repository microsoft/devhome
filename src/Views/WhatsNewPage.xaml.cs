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

    private async void ConnectToGitHubButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.GetService<IInfoBarService>().ShowAppLevelInfoBar(InfoBarSeverity.Warning, "Connecting", "To github");
        var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
        var pluginService = new PluginService();
        var plugins = pluginService.GetInstalledPluginsAsync(ProviderType.DeveloperId).Result;

        // TODO: Replace this with a check for KnownPluginsGuid got Github plugin
        var plugin = plugins.Where(p => p.Name.Contains("Github")).FirstOrDefault();

        if (plugin is null)
        {
            var connectToGitHubContentDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("WhatsNewPage_ConnectToGitHub_NoPluginsContentDialog_Title"),
                CloseButtonText = resourceLoader.GetString("WhatsNewPage_ConnectToGitHub_NoPluginsContentDialog_CloseButtonText"),
                XamlRoot = XamlRoot,
            };
            _ = await connectToGitHubContentDialog.ShowAsync();
            return;
        }

        var devIdProvider = await plugin.GetProviderAsync<IDeveloperIdProvider>();

        if (devIdProvider is null)
        {
            return;
        }

        if (devIdProvider.GetLoggedInDeveloperIds().Any())
        {
            // DevId already connected
            var connectToGitHubContentDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("WhatsNewPage_ConnectToGitHub_AlreadyConnectedContentDialog_Title"),
                CloseButtonText = resourceLoader.GetString("WhatsNewPage_ConnectToGitHub_AlreadyConnectedContentDialog_CloseButtonText"),
                XamlRoot = XamlRoot,
            };
            _ = await connectToGitHubContentDialog.ShowAsync();
            return;
        }

        try
        {
            await new AccountsProviderViewModel(devIdProvider).ShowLoginUIAsync("WhatsNew", this);
        }
        catch (Exception ex)
        {
            LoggerFactory.Get<ILogger>().LogError<string>($"ConnectToGitHubButton_Click_Failure", LogLevel.Local, $"Error: {ex}");
        }
    }
}
