// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.Views;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;
using Windows.ApplicationModel;

namespace DevHome.Settings.ViewModels;

public partial class ExtensionSettingsViewModel : ObservableObject
{
    public ExtensionSettingsViewModel()
    {
        Breadcrumbs = new ObservableCollection<Breadcrumb> { };
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get; set;
    }

    [RelayCommand]
    private async Task OnSettingsContentLoadedAsync(PluginAdaptiveCardPanel pluginAdaptiveCardPanel)
    {
        var pluginWrappers = Task.Run(async () =>
        {
            var pluginService = Application.Current.GetService<IPluginService>();
            return await pluginService.GetInstalledPluginsAsync(true);
        }).Result;

        var navigationService = Application.Current.GetService<INavigationService>();
        foreach (var pluginWrapper in pluginWrappers)
        {
            if ((navigationService.LastParameterUsed != null) &&
                ((string)navigationService.LastParameterUsed == pluginWrapper.PackageFullName))
            {
                var stringResource = new StringResource("DevHome.Settings/Resources");
                Breadcrumbs.Add(new Breadcrumb(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!));
                Breadcrumbs.Add(new Breadcrumb(stringResource.GetLocalized("Settings_Extensions_Header"), typeof(ExtensionsViewModel).FullName!));
                Breadcrumbs.Add(new Breadcrumb(pluginWrapper.Name, typeof(ExtensionSettingsViewModel).FullName!));

                var settingsProvider = Task.Run(() => pluginWrapper.GetProviderAsync<ISettingsProvider>()).Result;
                if (settingsProvider != null)
                {
                    var loginUIAdaptiveCardController = settingsProvider.GetAdaptiveCardController(new List<string>() { string.Empty }.ToArray());
                    var renderer = new AdaptiveCardRenderer();
                    renderer.HostConfig.ContainerStyles.Default.BackgroundColor = Microsoft.UI.Colors.Transparent;

                    pluginAdaptiveCardPanel.Bind(loginUIAdaptiveCardController, renderer);
                }
            }
        }

        await Task.CompletedTask;
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
