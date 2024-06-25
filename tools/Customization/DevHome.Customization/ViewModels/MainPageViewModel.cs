// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DevHome.Customization.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    private readonly DispatcherQueue _dispatcherQueue;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private bool _anyDevDrivesPresent;

    public MainPageViewModel(
        INavigationService navigationService,
        DispatcherQueue dispatcherQueue)
    {
        _navigationService = navigationService;
        _dispatcherQueue = dispatcherQueue;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs = [new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!)];
    }

    [RelayCommand]
    public async Task LoadViewModelContentAsync()
    {
        await Task.Run(async () =>
        {
            // Getting all dev drives can be an expensive operation so should not be queried on the UI thread.
            var anyDevDrivesPresent = Application.Current.GetService<IDevDriveManager>().GetAllDevDrivesThatExistOnSystem().Any();

            // Update the UI thread
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                AnyDevDrivesPresent = anyDevDrivesPresent;
            });
        });
    }

    [RelayCommand]
    private async Task LaunchWindowsDeveloperSettings()
    {
        await Windows.System.Launcher.LaunchUriAsync(new("ms-settings:developers"));
    }

    [RelayCommand]
    private void NavigateToFileExplorerPage()
    {
        _navigationService.NavigateTo(typeof(FileExplorerViewModel).FullName!);
    }

    [RelayCommand]
    private void NavigateToDevDriveInsightsPage()
    {
        _navigationService.NavigateTo(typeof(DevDriveInsightsViewModel).FullName!);
    }

    [RelayCommand]
    private void NavigateToVirtualizationFeatureManagementPage()
    {
        NavigationService.NavigateTo(typeof(VirtualizationFeatureManagementViewModel).FullName!);
    }

    [RelayCommand]
    private void NavigateToGeneralSystemPage()
    {
        NavigationService.NavigateTo(typeof(GeneralSystemViewModel).FullName!);
    }
}
