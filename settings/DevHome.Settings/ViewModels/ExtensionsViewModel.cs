// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DevHome.Settings.ViewModels;

public partial class ExtensionViewModel : ObservableObject
{
    private readonly Setting _setting;

    private readonly ExtensionsViewModel _extensionsViewModel;

    public ExtensionViewModel(Setting setting, ExtensionsViewModel extensionsViewModel)
    {
        _setting = setting;
        _extensionsViewModel = extensionsViewModel;
    }

    public string Path => _setting.Path;

    public string Header => _setting.Header;

    public string Description => _setting.Description;

    public bool HasToggleSwitch => _setting.HasToggleSwitch;

    public bool IsEnabled
    {
        get => _setting.IsExtensionEnabled;
        set => _setting.IsExtensionEnabled = value;
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        _extensionsViewModel.Navigate(_setting.Path);
    }
}

public partial class ExtensionsViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private ObservableCollection<ExtensionViewModel> _settingsList = new ();

    public ExtensionsViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        var pluginService = Application.Current.GetService<IPluginService>();
        pluginService.OnPluginsChanged -= OnPluginsChanged;
        pluginService.OnPluginsChanged += OnPluginsChanged;

        DisplaySettings();
    }

    private void DisplaySettings()
    {
        var pluginWrappers = Task.Run(async () =>
        {
            var pluginService = Application.Current.GetService<IPluginService>();
            return await pluginService.GetInstalledPluginsAsync(true);
        }).Result;

        SettingsList.Clear();

        foreach (var pluginWrapper in pluginWrappers)
        {
            // Don't show self as an extension
            if (Package.Current.Id.FullName == pluginWrapper.PackageFullName)
            {
                continue;
            }

            var setting = new Setting("Plugins/" + pluginWrapper.PackageFullName, pluginWrapper.PackageFullName, pluginWrapper.Name, string.Empty, string.Empty, true);
            SettingsList.Add(new ExtensionViewModel(setting, this));
        }
    }

    private async void OnPluginsChanged(object? sender, EventArgs e)
    {
        await _dispatcher.EnqueueAsync(() => { DisplaySettings(); });
    }

    public void Navigate(string path)
    {
        // TODO: Navigate to Plugin's settings Adaptive Card
        // https://github.com/microsoft/devhome/issues/608
    }
}
