// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Windows.System;

namespace DevHome.Customization.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public MainPageViewModel()
    {
        _navigationService = Application.Current.GetService<INavigationService>();
    }

    [RelayCommand]
    private async Task LaunchWindowsDeveloperSettings()
    {
        await Launcher.LaunchUriAsync(new("ms-settings:developers"));
    }

    [RelayCommand]
    private void NavigateToDeveloperFileExplorerPage()
    {
        _navigationService.NavigateTo(typeof(DeveloperFileExplorerViewModel).FullName!);
    }

    [RelayCommand]
    private void NavigateToDevDriveInsightsPage()
    {
        _navigationService.NavigateTo(typeof(DevDriveInsightsViewModel).FullName!);
    }
}
