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
using Microsoft.UI.Xaml;
using Windows.System;
using WinUIEx;

namespace DevHome.Customization.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private INavigationService NavigationService { get; }

    private readonly WindowEx _windowEx;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private bool _anyDevDrivesPresent;

    public MainPageViewModel(
        INavigationService navigationService,
        WindowEx windowEx)
    {
        NavigationService = navigationService;
        _windowEx = windowEx;

        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs = [new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!)];
    }

    public async Task LoadViewModelContentAsync()
    {
        await Task.Run(async () =>
        {
            // Getting all dev drives can be an expensive operation so should not be queried on the UI thread.
            var anyDevDrivesPresent = Application.Current.GetService<IDevDriveManager>().GetAllDevDrivesThatExistOnSystem().Any();

            // Update the UI thread
            await _windowEx.DispatcherQueue.EnqueueAsync(() =>
            {
                AnyDevDrivesPresent = anyDevDrivesPresent;
            });
        });
    }

    [RelayCommand]
    private async Task LaunchWindowsDeveloperSettings()
    {
        await Launcher.LaunchUriAsync(new("ms-settings:developers"));
    }

    [RelayCommand]
    private void NavigateToFileExplorerPage()
    {
        NavigationService.NavigateTo(typeof(FileExplorerViewModel).FullName!);
    }

    [RelayCommand]
    private void NavigateToDevDriveInsightsPage()
    {
        NavigationService.NavigateTo(typeof(DevDriveInsightsViewModel).FullName!);
    }
}
