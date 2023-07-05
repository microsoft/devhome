// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;

    public INavigationService NavigationService
    {
        get;
    }

    public INavigationViewService NavigationViewService
    {
        get;
    }

    [ObservableProperty]
    private object? _selected;

    [ObservableProperty]
    private InfoBarModel _shellInfoBarModel = new ();

    public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService, ILocalSettingsService localSettingsService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        _localSettingsService = localSettingsService;
    }

    public async Task OnLoaded()
    {
        if (await _localSettingsService.ReadSettingAsync<bool>(WellKnownSettingsKeys.IsNotFirstRun))
        {
            NavigationService.NavigateTo(NavigationService.DefaultPage);
        }
        else
        {
            NavigationService.NavigateTo(typeof(WhatsNewViewModel).FullName!);
        }
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (IsSettingsPage(e.SourcePageType.FullName))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }

    private bool IsSettingsPage(string? pageType)
    {
        if (string.IsNullOrEmpty(pageType))
        {
            return false;
        }

#pragma warning disable CA1310 // Specify StringComparison for correctness
        return pageType.StartsWith("DevHome.Settings");
#pragma warning restore CA1310 // Specify StringComparison for correctness
    }
}
