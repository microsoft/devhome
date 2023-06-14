// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DevHome.Settings.ViewModels;

public partial class PreferencesViewModel : ObservableObject
{
    private const string StartupTaskId = "DevHomeDefaultStartup";

    private readonly IThemeSelectorService _themeSelectorService;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private bool _launchAtStartup;

    [ObservableProperty]
    private bool _launchAtStartupButtonEnabled;

    public PreferencesViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;
    }

    public async Task OnPageLaunched()
    {
        var startupTask = await StartupTask.GetAsync(StartupTaskId);
        if (startupTask != null)
        {
            switch (startupTask.State)
            {
                case StartupTaskState.DisabledByUser:
                    LaunchAtStartupButtonEnabled = false;
                    LaunchAtStartup = false;
                    break;

                case StartupTaskState.DisabledByPolicy:
                    LaunchAtStartupButtonEnabled = false;
                    LaunchAtStartup = false;
                    break;

                case StartupTaskState.Disabled:
                    LaunchAtStartupButtonEnabled = true;
                    break;

                case StartupTaskState.Enabled:
                    LaunchAtStartupButtonEnabled = false;
                    LaunchAtStartup = true;
                    break;
            }
        }
    }

    [RelayCommand]
    private async Task SwitchThemeAsync(ElementTheme theme)
    {
        if (ElementTheme != theme)
        {
            ElementTheme = theme;

            await _themeSelectorService.SetThemeAsync(theme);
        }
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        var startup = StartupTask.GetAsync(StartupTaskId).GetAwaiter().GetResult();
        if (value && startup.State != StartupTaskState.Enabled)
        {
            LaunchAtStartupButtonEnabled = false;

            var newState = startup.RequestEnableAsync().GetAwaiter().GetResult();
            if (newState != StartupTaskState.Enabled)
            {
                LaunchAtStartup = false;
            }
        }
    }
}
