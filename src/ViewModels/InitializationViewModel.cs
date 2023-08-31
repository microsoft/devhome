// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.Views;
using Microsoft.UI.Xaml;

namespace DevHome.ViewModels;
public class InitializationViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelector;
    private readonly IPluginService _pluginService;

    public InitializationViewModel(IThemeSelectorService themeSelector, IPluginService pluginService)
    {
        _themeSelector = themeSelector;
        _pluginService = pluginService;
    }

    public void OnPageLoaded()
    {
        App.MainWindow.Content = Application.Current.GetService<ShellPage>();

        _themeSelector.SetRequestedTheme();
    }
}
