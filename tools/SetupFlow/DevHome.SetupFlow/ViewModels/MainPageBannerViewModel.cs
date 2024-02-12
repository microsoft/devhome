// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.SetupFlow.Services;
using DevHome.Telemetry;
using Windows.Storage;
using Windows.System;

namespace DevHome.SetupFlow.ViewModels;

public partial class MainPageBannerViewModel : ObservableObject
{
    private readonly SetupFlowOrchestrator _orchestrator;

    private const string _hideSetupFlowBannerKey = "HideSetupFlowBanner";

    [ObservableProperty]
    private bool _showBanner = true;

    public MainPageBannerViewModel(SetupFlowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        ShowBanner = ShouldShowSetupFlowBanner();
    }

    [RelayCommand]
    private async Task BannerButtonAsync()
    {
        await Launcher.LaunchUriAsync(new("https://go.microsoft.com/fwlink/?linkid=2235076"));
    }

    [RelayCommand]
    private void HideBanner()
    {
        TelemetryFactory.Get<ITelemetry>().LogCritical("MainPage_HideLearnMoreBanner_Event", false, _orchestrator.ActivityId);
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        roamingProperties[_hideSetupFlowBannerKey] = bool.TrueString;
        ShowBanner = false;
    }

    private bool ShouldShowSetupFlowBanner()
    {
        var roamingProperties = ApplicationData.Current.RoamingSettings.Values;
        return !roamingProperties.ContainsKey(_hideSetupFlowBannerKey);
    }
}
