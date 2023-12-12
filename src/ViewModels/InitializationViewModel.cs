// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Services;
using DevHome.Logging;
using DevHome.Views;
using Microsoft.UI.Xaml;

namespace DevHome.ViewModels;

public class InitializationViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelector;
    private readonly IWidgetHostingService _widgetHostingService;

    public InitializationViewModel(IThemeSelectorService themeSelector, IWidgetHostingService widgetHostingService)
    {
        _themeSelector = themeSelector;
        _widgetHostingService = widgetHostingService;
    }

    public async void OnPageLoaded()
    {
        try
        {
            await _widgetHostingService.EnsureWidgetServiceAsync();
        }
        catch (Exception ex)
        {
            GlobalLog.Logger?.ReportInfo("InitializationViewModel", "Installing WidgetService failed: ", ex);
        }

        App.MainWindow.Content = Application.Current.GetService<ShellPage>();

        _themeSelector.SetRequestedTheme();
    }
}
