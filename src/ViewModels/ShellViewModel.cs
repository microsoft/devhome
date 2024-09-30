// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using Serilog;

namespace DevHome.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IAppInfoService _appInfoService;
    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private string? _announcementText;

    public string Title => _appInfoService.GetAppNameLocalized();

    public INavigationService NavigationService { get; }

    public INavigationViewService NavigationViewService { get; }

    [ObservableProperty]
    private object? _selected;

    [ObservableProperty]
    private InfoBarModel _shellInfoBarModel = new();

    [ObservableProperty]
    private bool _isDevHomeGPOEnabled;

    public ShellViewModel(
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        ILocalSettingsService localSettingsService,
        IScreenReaderService screenReaderService,
        IAppInfoService appInfoService,
        IThemeSelectorService themeSelectorService)
    {
        NavigationService = navigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = navigationViewService;
        _localSettingsService = localSettingsService;
        _appInfoService = appInfoService;
        _themeSelectorService = themeSelectorService;

        screenReaderService.AnnouncementTextChanged += OnAnnouncementTextChanged;
    }

    public async Task OnLoaded()
    {
        var activationKind = AppInstance.GetCurrent().GetActivatedEventArgs().Kind;
        Log.Information($"Activated with kind {activationKind}");
        TelemetryFactory.Get<ITelemetry>().Log("DevHome_Shell_Loaded_Event", LogLevel.Critical, new DevHomeShellLoadedEvent(activationKind));

        IsDevHomeGPOEnabled = GPOHelper.GetConfiguredEnabledDevHomeValue();
        if (!IsDevHomeGPOEnabled)
        {
            return;
        }

        switch (activationKind)
        {
            case ExtendedActivationKind.File:
                // Allow the file activation handler to navigate to the appropriate page.
                break;
            case ExtendedActivationKind.Protocol:
                break;
            case ExtendedActivationKind.Launch:
            default:
                var isNotFirstRun = await _localSettingsService.ReadSettingAsync<bool>(WellKnownSettingsKeys.IsNotFirstRun);
                NavigationService.NavigateTo(isNotFirstRun ? NavigationService.DefaultPage : typeof(WhatsNewViewModel).FullName!);
                break;
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

        return pageType.StartsWith("DevHome.Settings", StringComparison.Ordinal);
    }

    private void OnAnnouncementTextChanged(object? sender, string text)
    {
        // Clear previous value to notify all bindings.
        // This allows announcing the same text consecutively multiple times.
        AnnouncementText = string.Empty;

        // Set new announcement title
        AnnouncementText = text;
    }

    internal void NotifyActualThemeChanged()
    {
        _themeSelectorService.SetRequestedTheme();
    }
}
