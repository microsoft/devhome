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
using DevHome.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class ExtensionSettingsViewModel : ObservableObject
{
    private readonly IExtensionService _extensionService;
    private readonly INavigationService _navigationService;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ExtensionSettingsViewModel(IExtensionService extensionService, INavigationService navigationService)
    {
        _extensionService = extensionService;
        _navigationService = navigationService;

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
                        GlobalLog.Logger?.ReportError($"{adaptiveCardSessionResult.Result.DisplayMessage}" +
                            $" - {adaptiveCardSessionResult.Result.DiagnosticText}");
                        await Task.CompletedTask;
                    }

                    var adaptiveCardSession = adaptiveCardSessionResult.AdaptiveCardSession;
                    var renderer = new AdaptiveCardRenderer();
                    renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

                    extensionAdaptiveCardPanel.Bind(adaptiveCardSession, renderer);
                }
            }
        }

        await Task.CompletedTask;
    }

    public void FillBreadcrumbBar(string lastCrumbName)
    {
        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs.Add(new(stringResource.GetLocalized("Settings_Extensions_Header"), typeof(ExtensionLibraryViewModel).FullName!));
        Breadcrumbs.Add(new Breadcrumb(lastCrumbName, typeof(ExtensionSettingsViewModel).FullName!));
    }

    public void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
    }
}
