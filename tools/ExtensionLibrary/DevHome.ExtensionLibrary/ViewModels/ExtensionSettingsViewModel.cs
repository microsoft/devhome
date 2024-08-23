// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Contracts.Services;
using DevHome.ExtensionLibrary.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
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
    private bool _isAdaptiveCardEnabled;

    [ObservableProperty]
    private bool _isWebView2Enabled;

    public event Action? SettingsContentLoaded;

    public ExtensionSettingsViewModel(
        IExtensionService extensionService,
        INavigationService navigationService,
        AdaptiveCardRenderingService adaptiveCardRenderingService,
        IThemeSelectorService themeSelectorService)
    {
        _extensionService = extensionService;
        _navigationService = navigationService;
        _adaptiveCardRenderingService = adaptiveCardRenderingService;
        _webMessageReceived = string.Empty;
        _webViewUrl = null;
        Breadcrumbs = new ObservableCollection<Breadcrumb>();
    }

    [RelayCommand]
    private async Task OnSettingsContentLoadedAsync(ExtensionAdaptiveCardPanel extensionAdaptiveCardPanel)
    {
        var extensionWrappers = await _extensionService.GetInstalledExtensionsAsync(true);

        foreach (var extensionWrapper in extensionWrappers)
        {
            if ((_navigationService.LastParameterUsed != null) &&
                ((string)_navigationService.LastParameterUsed == extensionWrapper.ExtensionUniqueId))
            {
                FillBreadcrumbBar(extensionWrapper.ExtensionDisplayName);

                var settingsProvider = Task.Run(() => extensionWrapper.GetProviderAsync<ISettingsProvider>()).Result;
                if (settingsProvider != null)
                {
                    if (settingsProvider is ISettingsProvider2 settingsProvider2)
                    {
                        IsAdaptiveCardEnabled = false;
                        IsWebView2Enabled = true;
                        var webViewUrl = settingsProvider2.GetSettingsWebView();
                        if (webViewUrl != null)
                        {
                            WebViewUrl = new Uri(webViewUrl.Url);
                        }
                    }
                    else
                    {
                        IsAdaptiveCardEnabled = true;
                        IsWebView2Enabled = false;
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
}
