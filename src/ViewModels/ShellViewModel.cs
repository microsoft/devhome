// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Logging;
using Microsoft.UI.Xaml.Navigation;

namespace DevHome.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IAppInfoService _appInfoService;

    [ObservableProperty]
    private string? _announcementText;

    [ObservableProperty]
    private bool isBackEnabled;

    public string Title => _appInfoService.GetAppNameLocalized();

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

    public ShellViewModel(
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        ILocalSettingsService localSettingsService,
        IScreenReaderService screenReaderService,
        IAppInfoService appInfoService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        _localSettingsService = localSettingsService;
        _appInfoService = appInfoService;

        screenReaderService.AnnouncementTextChanged += OnAnnouncementTextChanged;
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
        IsBackEnabled = IsSetupFlowPage(e.SourcePageType.FullName) ? false : NavigationService.CanGoBack;

        if (IsExtensionSettingsPage(e.SourcePageType.FullName))
        {
            // If we navigate to the L3 settings page for an extension, leave the selected NavigationView item as it
            // was, since we might be coming from Settings or Extensions.
            return;
        }
        else if (IsSettingsPage(e.SourcePageType.FullName))
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

    private bool IsExtensionSettingsPage(string? pageType)
    {
        if (string.IsNullOrEmpty(pageType))
        {
            return false;
        }

        return pageType.StartsWith("DevHome.Settings.Views.ExtensionSettingsPage", StringComparison.Ordinal);
    }

    private bool IsSettingsPage(string? pageType)
    {
        if (string.IsNullOrEmpty(pageType))
        {
            return false;
        }

        return pageType.StartsWith("DevHome.Settings", StringComparison.Ordinal);
    }

    private bool IsSetupFlowPage(string? pageType)
    {
        if (string.IsNullOrEmpty(pageType))
        {
            return false;
        }

        return pageType.StartsWith("DevHome.SetupFlow.Views", StringComparison.Ordinal) &&
            !pageType.Equals("DevHome.SetupFlow.Views.SetupFlowPage", StringComparison.Ordinal);
    }

    private void OnAnnouncementTextChanged(object? sender, string text)
    {
        // Clear previous value to notify all bindings.
        // This allows announcing the same text consecutively multiple times.
        AnnouncementText = string.Empty;

        // Set new announcement title
        AnnouncementText = text;
    }
}
