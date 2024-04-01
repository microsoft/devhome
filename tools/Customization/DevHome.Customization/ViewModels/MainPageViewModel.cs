// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Windows.System;

namespace DevHome.Customization.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private INavigationService NavigationService { get; }

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public MainPageViewModel(
        INavigationService navigationService)
    {
        NavigationService = navigationService;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs = [new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!)];
    }

    [RelayCommand]
    private async Task LaunchWindowsDeveloperSettings()
    {
        await Launcher.LaunchUriAsync(new("ms-settings:developers"));
    }

    [RelayCommand]
    private void NavigateToDeveloperFileExplorerPage()
    {
        NavigationService.NavigateTo(typeof(DeveloperFileExplorerViewModel).FullName!);
    }

    [RelayCommand]
    private void NavigateToDevDriveInsightsPage()
    {
        NavigationService.NavigateTo(typeof(DevDriveInsightsViewModel).FullName!);
    }
}
