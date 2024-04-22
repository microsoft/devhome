// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;

namespace DevHome.Common.Models;

public partial class ExperimentalFeature : ObservableObject
{
    private readonly bool _isEnabledByDefault;

    [ObservableProperty]
    private bool _isEnabled;

    public string Id { get; init; }

    public bool IsVisible { get; init; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public ExperimentalFeature(string id, bool enabledByDefault, bool visible = true)
    {
        Id = id;
        _isEnabledByDefault = enabledByDefault;
        IsVisible = visible;

        IsEnabled = CalculateEnabled();
    }

    public bool CalculateEnabled()
    {
        if (LocalSettingsService!.HasSettingAsync($"ExperimentalFeature_{Id}").Result)
        {
            return LocalSettingsService.ReadSettingAsync<bool>($"ExperimentalFeature_{Id}").Result;
        }

        return _isEnabledByDefault;
    }

    public string Name
    {
        get
        {
            var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
            return stringResource.GetLocalized(Id + "_Name");
        }
    }

    public string Description
    {
        get
        {
            var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
            return stringResource.GetLocalized(Id + "_Description");
        }
    }

    [RelayCommand]
    public async Task OnToggledAsync()
    {
        IsEnabled = !IsEnabled;

        await LocalSettingsService!.SaveSettingAsync($"ExperimentalFeature_{Id}", IsEnabled);

        await LocalSettingsService!.SaveSettingAsync($"IsSeeker", true);

        TelemetryFactory.Get<ITelemetry>().Log("ExperimentalFeature_Toggled_Event", LogLevel.Critical, new ExperimentalFeatureEvent(Id, IsEnabled));
    }
}
