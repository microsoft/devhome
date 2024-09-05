// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;

namespace DevHome.Common.Models;

public partial class ExperimentalFeature : ObservableObject
{
    private readonly bool _isEnabledByDefault;

    [ObservableProperty]
    private bool _isEnabled;

    public string Id { get; init; }

    public string OpenPageKey { get; init; }

    public string OpenPageParameter { get; init; }

    public bool NeedsFeaturePresenceCheck { get; init; }

    public bool IsVisible { get; init; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public static IQuickstartSetupService? QuickstartSetupService { get; set; }

    public ExperimentalFeature(string id, bool enabledByDefault, bool needsFeaturePresenceCheck, string openPageKey, string openPageParameter, bool visible = true)
    {
        Id = id;
        OpenPageKey = openPageKey;
        OpenPageParameter = openPageParameter;
        _isEnabledByDefault = enabledByDefault;
        NeedsFeaturePresenceCheck = needsFeaturePresenceCheck;
        IsVisible = visible;

        IsEnabled = CalculateEnabled();
    }

    public bool CalculateEnabled()
    {
        if (LocalSettingsService!.HasSettingAsync($"ExperimentalFeature_{Id}").Result)
        {
            return LocalSettingsService.ReadSettingAsync($"ExperimentalFeature_{Id}", ExperimentalFeatureSourceGenerationContext.Default.Boolean).Result;
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

        await LocalSettingsService!.SaveSettingAsync($"ExperimentalFeature_{Id}", IsEnabled, ExperimentalFeatureSourceGenerationContext.Default.Boolean);

        await LocalSettingsService!.SaveSettingAsync($"IsSeeker", true, ExperimentalFeatureSourceGenerationContext.Default.Boolean);

        TelemetryFactory.Get<ITelemetry>().Log("ExperimentalFeature_Toggled_Event", LogLevel.Critical, new ExperimentalFeatureEvent(Id, IsEnabled));

        // To simplify setup for the Quickstart experimental feature, install the associated Dev Home Azure Extension if it's not already present
        // when that feature is enabled. Those operations will only occur on Canary and Stable builds of Dev Home.
        if (string.Equals(Id, "QuickstartPlayground", StringComparison.Ordinal) && IsEnabled)
        {
            if (!QuickstartSetupService!.IsDevHomeAzureExtensionInstalled())
            {
                await QuickstartSetupService!.InstallDevHomeAzureExtensionAsync();
            }
        }
    }

    [RelayCommand]
    public void Open()
    {
        if (OpenPageKey != null)
        {
            Application.Current.GetService<INavigationService>().NavigateTo(OpenPageKey, OpenPageParameter);
        }
    }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(bool))]
internal sealed partial class ExperimentalFeatureSourceGenerationContext : JsonSerializerContext
{
}
