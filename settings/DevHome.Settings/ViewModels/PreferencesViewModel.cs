// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DevHome.Settings.ViewModels;

public partial class PreferencesViewModel : ObservableRecipient
{
    private readonly IThemeSelectorService _themeSelectorService;
    private ElementTheme _elementTheme;

    public ElementTheme ElementTheme
    {
        get => _elementTheme;
        set => SetProperty(ref _elementTheme, value);
    }

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public PreferencesViewModel(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });
    }
}
