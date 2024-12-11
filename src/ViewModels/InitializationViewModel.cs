// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Services;
using DevHome.Services.Core.Contracts;
using DevHome.Telemetry;
using DevHome.TelemetryEvents;
using DevHome.Views;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.ViewModels;

public class InitializationViewModel : ObservableObject
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(InitializationViewModel));

    private readonly IThemeSelectorService _themeSelector;
    private readonly IWidgetServiceService _widgetServiceService;

    public InitializationViewModel(
        IThemeSelectorService themeSelector,
        IWidgetServiceService widgetServiceService)
    {
        _themeSelector = themeSelector;
        _widgetServiceService = widgetServiceService;
    }

    public async void OnPageLoaded()
    {
        TelemetryFactory.Get<ITelemetry>().Log("DevHome_Initialization_Started_Event", LogLevel.Critical, new DevHomeInitializationStartedEvent());
        _log.Information("Dev Home Initialization starting.");

        // Install the widget service if we're on Windows 10 and it's not already installed.
        try
        {
            var widgetStatus = _widgetServiceService.GetWidgetServiceState();
            if (widgetStatus != WidgetServiceService.WidgetServiceStates.NotAtMinVersion)
            {
                _log.Information("Skipping installing WidgetService, already installed.");
            }
            else
            {
                if (!RuntimeHelper.IsOnWindows11)
                {
                    // We're on Windows 10 and don't have the widget service, try to install it.
                    await _widgetServiceService.TryInstallingWidgetService();
                }
            }
        }
        catch (Exception ex)
        {
            _log.Information(ex, "Installing WidgetService failed: ");
        }

        App.MainWindow.Content = Application.Current.GetService<ShellPage>();

        _themeSelector.SetRequestedTheme();

        TelemetryFactory.Get<ITelemetry>().Log("DevHome_Initialization_Ended_Event", LogLevel.Critical, new DevHomeInitializationEndedEvent());
        _log.Information("Dev Home Initialization ended.");
    }
}
