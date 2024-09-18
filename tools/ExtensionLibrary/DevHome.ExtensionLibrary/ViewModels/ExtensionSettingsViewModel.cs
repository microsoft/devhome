// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.Views;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class ExtensionSettingsViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExtensionSettingsViewModel));

    private readonly IExtensionService _extensionService;
    private readonly INavigationService _navigationService;
    private readonly AdaptiveCardRenderingService _adaptiveCardRenderingService;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private string _webMessageReceived;

    [ObservableProperty]
    private Uri? _webViewUrl;

    [ObservableProperty]
    private bool _isAdaptiveCardVisible;

    [ObservableProperty]
    private bool _isWebView2Visible;

    [ObservableProperty]
    private ISettingsProvider? _extensionSettingsProvider;

    [ObservableProperty]
    private ExtensionAdaptiveCardPanel? _settingsExtensionAdaptiveCardPanel;

    public event Action? SettingsContentLoaded;

    public ExtensionSettingsViewModel(
        IExtensionService extensionService,
        INavigationService navigationService,
        AdaptiveCardRenderingService adaptiveCardRenderingService)
    {
        _extensionService = extensionService;
        _navigationService = navigationService;
        _adaptiveCardRenderingService = adaptiveCardRenderingService;
        _webMessageReceived = string.Empty;
        _webViewUrl = null;
        _isWebView2Visible = false;
        _isAdaptiveCardVisible = false;
        _extensionSettingsProvider = null;
        _settingsExtensionAdaptiveCardPanel = null;
        Breadcrumbs = new ObservableCollection<Breadcrumb>();
    }

    [RelayCommand]
    private async Task OnSettingsContentLoadedAsync(ExtensionAdaptiveCardPanel extensionAdaptiveCardPanel)
    {
        SettingsExtensionAdaptiveCardPanel = extensionAdaptiveCardPanel;
        var extensionWrappers = await _extensionService.GetInstalledExtensionsAsync(true);

        foreach (var extensionWrapper in extensionWrappers)
        {
            if ((_navigationService.LastParameterUsed != null) &&
                ((string)_navigationService.LastParameterUsed == extensionWrapper.ExtensionUniqueId))
            {
                FillBreadcrumbBar(extensionWrapper.ExtensionDisplayName);

                var settingsProvider = default(ISettingsProvider);

                try
                {
                    settingsProvider = Task.Run(() => extensionWrapper.GetProviderAsync<ISettingsProvider>()).Result;
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error getting settings provider: {ex.Message}");
                }

                ExtensionSettingsProvider = settingsProvider;
                if (settingsProvider != null)
                {
                    if (settingsProvider is ISettingsProvider2 settingsProvider2)
                    {
                        try
                        {
                            RenderWebView2(settingsProvider2);
                        }
                        catch (NotImplementedException notImplementedException)
                        {
                            _log.Error(notImplementedException, $"Error loading WebView2: {notImplementedException.Message}");
                            RenderAdaptiveCard(settingsProvider, extensionAdaptiveCardPanel);
                        }
                    }
                    else
                    {
                        RenderAdaptiveCard(settingsProvider, extensionAdaptiveCardPanel);
                    }
                }
            }
        }

        SettingsContentLoaded?.Invoke();
        await Task.CompletedTask;
    }

    private void FillBreadcrumbBar(string lastCrumbName)
    {
        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs.Add(new(stringResource.GetLocalized("Settings_Extensions_Header"), typeof(ExtensionLibraryViewModel).FullName!));
        Breadcrumbs.Add(new Breadcrumb(lastCrumbName, typeof(ExtensionSettingsViewModel).FullName!));
    }

    internal void RenderWebView2(ISettingsProvider2 settingsProvider2)
    {
        ShowWebView2Grid();
        var webViewResult = settingsProvider2.GetSettingsWebView();
        WebViewUrl = new Uri(webViewResult.Url);
    }

    // Only called if the WebView2 receives a message from the web page
    public void RenderAdaptiveCard()
    {
        if (ExtensionSettingsProvider != null && SettingsExtensionAdaptiveCardPanel != null)
        {
            RenderAdaptiveCard(ExtensionSettingsProvider, SettingsExtensionAdaptiveCardPanel);
        }
    }

    internal async void RenderAdaptiveCard(ISettingsProvider settingsProvider, ExtensionAdaptiveCardPanel extensionAdaptiveCardPanel)
    {
        ShowAdaptiveCardPanel();
        var adaptiveCardSessionResult = settingsProvider.GetSettingsAdaptiveCardSession();
        if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
        {
            _log.Error($"{adaptiveCardSessionResult.Result.DisplayMessage}" +
                $" - {adaptiveCardSessionResult.Result.DiagnosticText}");
            await Task.CompletedTask;
        }

        var adaptiveCardSession = adaptiveCardSessionResult.AdaptiveCardSession;
        var renderer = await _adaptiveCardRenderingService.GetRendererAsync();
        renderer.HostConfig.Actions.ActionAlignment = ActionAlignment.Left;
        extensionAdaptiveCardPanel.Bind(adaptiveCardSession, renderer);
    }

    private void ShowWebView2Grid()
    {
        IsAdaptiveCardVisible = false;
        IsWebView2Visible = true;
    }

    private void ShowAdaptiveCardPanel()
    {
        IsWebView2Visible = false;
        IsAdaptiveCardVisible = true;
    }
}
