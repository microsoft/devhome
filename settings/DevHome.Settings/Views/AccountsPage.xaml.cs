// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Renderers;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.DeveloperId;
using DevHome.Common.Views;
using DevHome.Logging;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using DevHome.Telemetry;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Windows.Storage;
using WinUIEx;

namespace DevHome.Settings.Views;

public sealed partial class AccountsPage : Page
{
    public AccountsViewModel ViewModel { get; }

    public AccountsPage()
    {
        ViewModel = Application.Current.GetService<AccountsViewModel>();
        this.InitializeComponent();
    }

    private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var numProviders = ViewModel.AccountsProviders.Count;
        if (numProviders == 1)
        {
            await InitiateAddAccountUserExperienceAsync(this, ViewModel.AccountsProviders[0]);
        }
        else if (numProviders > 1)
        {
            AccountsProvidersFlyout.ShowAt(sender as Button);
        }
        else
        {
            var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
            var noProvidersContentDialog = new ContentDialog
            {
                Title = stringResource.GetLocalized("Settings_Accounts_NoProvidersContentDialog_Title"),
                Content = stringResource.GetLocalized("Settings_Accounts_NoProvidersContentDialog_Content"),
                PrimaryButtonText = stringResource.GetLocalized("Settings_Accounts_NoProvidersContentDialog_PrimaryButtonText"),
                PrimaryButtonCommand = FindExtensionsCommand,
                PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
                SecondaryButtonText = stringResource.GetLocalized("Settings_Accounts_NoProvidersContentDialog_SecondaryButtonText"),
                XamlRoot = XamlRoot,
            };
            await noProvidersContentDialog.ShowAsync();
        }
    }

    [RelayCommand]
    private void FindExtensions()
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        navigationService.NavigateTo(KnownPageKeys.Extensions);
    }

    private async void AddDeveloperId_Click(object sender, RoutedEventArgs e)
    {
        if (sender as Button is Button addAccountButton)
        {
            if (addAccountButton.Tag is AccountsProviderViewModel accountProvider)
            {
                await InitiateAddAccountUserExperienceAsync(this, accountProvider);
            }
            else
            {
                GlobalLog.Logger?.ReportInfo($"AddAccount_Click(): addAccountButton.Tag is not AccountsProviderViewModel - Sender: {sender} RoutedEventArgs: {e}");
                return;
            }
        }
    }

    public async Task ShowLoginUIAsync(string loginEntryPoint, Page parentPage, AccountsProviderViewModel accountProvider)
    {
        try
        {
            var adaptiveCardSessionResult = accountProvider.DeveloperIdProvider.GetLoginAdaptiveCardSession();
            if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
            {
                GlobalLog.Logger?.ReportError($"{adaptiveCardSessionResult.Result.DisplayMessage} - {adaptiveCardSessionResult.Result.DiagnosticText}");
                return;
            }

            var loginUIAdaptiveCardController = adaptiveCardSessionResult.AdaptiveCardSession;
            var extensionAdaptiveCardPanel = new ExtensionAdaptiveCardPanel();
            var renderer = new AdaptiveCardRenderer();
            await ConfigureLoginUIRenderer(renderer);
            renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

            extensionAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, renderer);
            extensionAdaptiveCardPanel.RequestedTheme = parentPage.ActualTheme;

            var loginUIContentDialog = new LoginUIDialog(extensionAdaptiveCardPanel)
            {
                XamlRoot = parentPage.XamlRoot,
                RequestedTheme = parentPage.ActualTheme,
            };

            await loginUIContentDialog.ShowAsync();

            // TODO: Await Login event to match up the loginEntryPoint and return DeveloperId
            // https://github.com/microsoft/devhome/issues/607
            loginUIAdaptiveCardController.Dispose();
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportError($"ShowLoginUIAsync(): loginUIContentDialog failed.", ex);
        }

        accountProvider.RefreshLoggedInAccounts();
    }

    private async Task ConfigureLoginUIRenderer(AdaptiveCardRenderer renderer)
    {
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        // Add custom Adaptive Card renderer for LoginUI as done for Widgets.
        renderer.ElementRenderers.Set(LabelGroup.CustomTypeString, new LabelGroupRenderer());
        renderer.ElementRenderers.Set("Input.ChoiceSet", new AccessibleChoiceSet());

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
            GlobalLog.Logger?.ReportError($"Failure occurred while retrieving the HostConfig file - HostConfigFileName: {hostConfigFileName}.", ex);
        }

        // Add host config for current theme to renderer
        dispatcher.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(hostConfigContents))
            {
                renderer.HostConfig = AdaptiveHostConfig.FromJsonString(hostConfigContents).HostConfig;

                // Remove margins from selectAction.
                renderer.AddSelectActionMargin = false;
            }
            else
            {
                GlobalLog.Logger?.ReportInfo($"HostConfig file contents are null or empty - HostConfigFileContents: {hostConfigContents}");
            }
        });
        return;
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        var confirmLogoutContentDialog = new ContentDialog
        {
            Title = stringResource.GetLocalized("Settings_Accounts_ConfirmLogoutContentDialog_Title"),
            Content = stringResource.GetLocalized("Settings_Accounts_ConfirmLogoutContentDialog_Content"),
            PrimaryButtonText = stringResource.GetLocalized("Settings_Accounts_ConfirmLogoutContentDialog_PrimaryButtonText"),
            SecondaryButtonText = stringResource.GetLocalized("Settings_Accounts_ConfirmLogoutContentDialog_SecondaryButtonText"),
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
                Title = stringResource.GetLocalized("Settings_Accounts_AfterLogoutContentDialog_Title"),
                Content = $"{accountToRemove.LoginId} " + stringResource.GetLocalized("Settings_Accounts_AfterLogoutContentDialog_Content"),
                CloseButtonText = stringResource.GetLocalized("Settings_Accounts_AfterLogoutContentDialog_PrimaryButtonText"),
                XamlRoot = XamlRoot,
                RequestedTheme = ActualTheme,
            };
            _ = await afterLogoutContentDialog.ShowAsync();
        }
    }

    private async Task InitiateAddAccountUserExperienceAsync(Page parentPage, AccountsProviderViewModel accountProvider)
    {
        TelemetryFactory.Get<ITelemetry>().Log(
                                                "EntryPoint_DevId_Event",
                                                LogLevel.Critical,
                                                new EntryPointEvent(EntryPointEvent.EntryPoint.Settings));

        var authenticationFlow = accountProvider.DeveloperIdProvider.GetAuthenticationExperienceKind();
        if (authenticationFlow == AuthenticationExperienceKind.CardSession)
        {
            await ShowLoginUIAsync("Settings", parentPage, accountProvider);
        }
        else if (authenticationFlow == AuthenticationExperienceKind.CustomProvider)
        {
            var windowHandle = Application.Current.GetService<WindowEx>().GetWindowHandle();
            var windowPtr = Win32Interop.GetWindowIdFromWindow(windowHandle);
            try
            {
                var developerIdResult = await accountProvider.DeveloperIdProvider.ShowLogonSession(windowPtr);
                if (developerIdResult.Result.Status == ProviderOperationStatus.Failure)
                {
                    GlobalLog.Logger?.ReportError($"{developerIdResult.Result.DisplayMessage} - {developerIdResult.Result.DiagnosticText}");
                    return;
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Logger?.ReportError($"Exception thrown while calling {nameof(accountProvider.DeveloperIdProvider)}.{nameof(accountProvider.DeveloperIdProvider.ShowLogonSession)}: ", ex);
            }

            accountProvider.RefreshLoggedInAccounts();
        }
    }
}
