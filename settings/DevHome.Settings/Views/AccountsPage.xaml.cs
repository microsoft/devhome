// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Extensions;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.DevHome.SDK;
using Windows.Storage;

namespace DevHome.Settings.Views;

public sealed partial class AccountsPage : Page
{
    public AccountsViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get;
    }

    public AccountsPage()
    {
        ViewModel = Application.Current.GetService<AccountsViewModel>();
        this.InitializeComponent();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new Breadcrumb(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new Breadcrumb(stringResource.GetLocalized("Settings_Accounts_Header"), typeof(AccountsViewModel).FullName!),
        };
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
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
                    await ShowLoginUIAsync("Settings", this, accountProvider);
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

    public async Task ShowLoginUIAsync(string loginEntryPoint, Page parentPage, AccountsProviderViewModel accountProvider)
    {
        string[] args = { loginEntryPoint };
        var loginUIAdaptiveCardController = accountProvider.DeveloperIdProvider.GetAdaptiveCardController(args);
        var pluginAdaptiveCardPanel = new PluginAdaptiveCardPanel();
        var renderer = new AdaptiveCardRenderer();
        await ConfigureLoginUIRenderer(renderer);
        renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

        pluginAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, renderer);
        pluginAdaptiveCardPanel.RequestedTheme = parentPage.ActualTheme;

        var loginUIContentDialog = new LoginUIDialog(pluginAdaptiveCardPanel)
        {
            XamlRoot = parentPage.XamlRoot,
            RequestedTheme = parentPage.ActualTheme,
        };

        await loginUIContentDialog.ShowAsync();
        accountProvider.RefreshLoggedInAccounts();

        // TODO: Await Login event to match up the loginEntryPoint and return DeveloperId
        loginUIAdaptiveCardController.Dispose();
    }

    private async Task ConfigureLoginUIRenderer(AdaptiveCardRenderer renderer)
    {
        Microsoft.UI.Dispatching.DispatcherQueue dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // Add custom Adaptive Card renderer for LoginUI as done for Widgets.
        renderer.ElementRenderers.Set(LabelGroup.CustomTypeString, new LabelGroupRenderer());

        var hostConfigContents = string.Empty;
        var hostConfigFileName = (ActualTheme == ElementTheme.Light) ? "LightHostConfig.json" : "DarkHostConfig.json";
        try
        {
            var uri = new Uri($"ms-appx:////DevHome.Settings/Assets/{hostConfigFileName}");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri).AsTask().ConfigureAwait(false);
            hostConfigContents = await FileIO.ReadTextAsync(file);
        }
        catch (Exception ex)
        {
            LoggerFactory.Get<ILogger>().Log($"Failure occurred while retrieving the HostConfig file", LogLevel.Local, $"Error: {ex} HostConfigFileName: {hostConfigFileName}");
        }

        // Add host config for current theme to renderer
        dispatcher.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(hostConfigContents))
            {
                renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;
            }
            else
            {
                LoggerFactory.Get<ILogger>().Log($"HostConfig file contents are null or empty", LogLevel.Local, $"HostConfigFileContents: {hostConfigContents}");
            }
        });
        return;
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
            RequestedTheme = ActualTheme,
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
                RequestedTheme = ActualTheme,
            };
            _ = await afterLogoutContentDialog.ShowAsync();
        }
    }
}
