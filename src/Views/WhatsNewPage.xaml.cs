// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Models;
using DevHome.Services;
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

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
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
        var pluginService = new PluginService();
        var plugins = pluginService.GetInstalledPluginsAsync(ProviderType.DevId).Result;

        var plugin = plugins.Where(p => p.Name.Contains("Github")).FirstOrDefault();

        if (plugin is null)
        {
            // Nothing to do if there are no plugins for GitHub
            return;
        }

        if (!plugin.IsRunning())
        {
            await plugin.StartPlugin();
        }

        var pluginObj = plugin.GetPluginObject();
        var devIdProvider = pluginObj?.GetProvider(ProviderType.DevId);

        if (devIdProvider is IDevIdProvider iDevIdProvider)
        {
            if (iDevIdProvider.GetLoggedInDeveloperIds().Any())
            {
                // DevId already connected
                var connectToGitHubContentDialog = new ContentDialog
                {
                    Title = "You are already connected to GitHub!",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot,
                };
                _ = await connectToGitHubContentDialog.ShowAsync();
                return;
            }

            try
            {
                // TODO: Replace this flow with LoginUI
                var devId = await iDevIdProvider.LoginNewDeveloperIdAsync();

                var connectToGitHubSuccessContentDialog = new ContentDialog
                {
                    Title = $"{devId.LoginId()} has connected to GitHub!",
                    CloseButtonText = "OK",
                    XamlRoot = XamlRoot,
                };
                _ = await connectToGitHubSuccessContentDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                LoggerFactory.Get<ILogger>().LogError<string>($"ConnectToGitHubButton_Click_Failure", LogLevel.Local, $"Error: {ex}");
            }
        }
    }
}
