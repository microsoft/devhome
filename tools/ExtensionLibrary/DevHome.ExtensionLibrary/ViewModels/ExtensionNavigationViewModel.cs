// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
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

public partial class ExtensionNavigationViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ExtensionNavigationViewModel));

    private readonly IExtensionService _extensionService;
    private readonly INavigationService _navigationService;
    private readonly AdaptiveCardRenderingService _adaptiveCardRenderingService;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ExtensionNavigationViewModel(
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
    private async Task OnNavigationContentLoadedAsync(ExtensionAdaptiveCardPanel extensionAdaptiveCardPanel)
    {
        try
        {
            var extensionWrappers = await _extensionService.GetInstalledExtensionsAsync(true);

            foreach (var extensionWrapper in extensionWrappers)
            {
                if ((_navigationService.LastParameterUsed != null) &&
                    ((string)_navigationService.LastParameterUsed == extensionWrapper.ExtensionUniqueId))
                {
                    FillBreadcrumbBar(extensionWrapper.ExtensionDisplayName);

                    var navigationProvider = Task.Run(() => extensionWrapper.GetProviderAsync<INavigationProvider>()).Result;
                    if (navigationProvider is null)
                    {
                        _log.Error($"Navigation Provider is null.");
                        return;
                    }

                    var pagesResult = await navigationProvider.GetNavigationPagesAsync();
                    if (pagesResult.Result.Status == ProviderOperationStatus.Failure)
                    {
                        _log.Error($"{pagesResult.Result.DisplayMessage}" +
                            $" - {pagesResult.Result.DiagnosticText}");
                        return;
                    }

                    var firstPage = pagesResult.NavigationPages.FirstOrDefault();
                    if (firstPage is null)
                    {
                        _log.Error($"{navigationProvider.DisplayName} has no pages.");
                        return;
                    }

                    var adaptiveCardSessionResult = firstPage!.GetNavigationAdaptiveCardSession();
                    if (adaptiveCardSessionResult.Result.Status == ProviderOperationStatus.Failure)
                    {
                        _log.Error($"{adaptiveCardSessionResult.Result.DisplayMessage}" +
                            $" - {adaptiveCardSessionResult.Result.DiagnosticText}");
                        return;
                    }

                    var adaptiveCardSession = adaptiveCardSessionResult.AdaptiveCardSession;
                    var renderer = await _adaptiveCardRenderingService.GetRendererAsync();
                    renderer.HostConfig.Actions.ActionAlignment = ActionAlignment.Left;

                    extensionAdaptiveCardPanel.Bind(adaptiveCardSession, renderer);
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed loading NavigationViewModel");
        }
    }

    private void FillBreadcrumbBar(string lastCrumbName)
    {
        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs.Add(new(stringResource.GetLocalized("Settings_Extensions_Header"), typeof(ExtensionLibraryViewModel).FullName!));
        Breadcrumbs.Add(new Breadcrumb(lastCrumbName, typeof(ExtensionNavigationViewModel).FullName!));
    }
}
