// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
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

    public ExtensionSettingsViewModel(
        IExtensionService extensionService,
        INavigationService navigationService,
        AdaptiveCardRenderingService adaptiveCardRenderingService)
    {
        _extensionService = extensionService;
        _navigationService = navigationService;
        _adaptiveCardRenderingService = adaptiveCardRenderingService;

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
                FillBreadcrumbBar(extensionWrapper.Name);

                var settingsProvider = Task.Run(() => extensionWrapper.GetProviderAsync<ISettingsProvider>()).Result;
                if (settingsProvider != null)
                {
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

        await Task.CompletedTask;
    }

    private void FillBreadcrumbBar(string lastCrumbName)
    {
        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs.Add(new(stringResource.GetLocalized("Settings_Extensions_Header"), typeof(ExtensionLibraryViewModel).FullName!));
        Breadcrumbs.Add(new Breadcrumb(lastCrumbName, typeof(ExtensionSettingsViewModel).FullName!));
    }
}
