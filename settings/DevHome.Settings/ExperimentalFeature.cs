// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Services;

namespace DevHome.Settings;

public partial class ExperimentalFeature : ObservableObject
{
    public string Id { get; init; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public ExperimentalFeature(string id)
    {
        Id = id;
        IsEnabled = GetIsEnabled();
    }

    public string Name
    {
        get
        {
            var stringResource = new StringResource("DevHome.Settings/Resources");
            return stringResource.GetLocalized(Id + "_Name");
        }
    }

    public string Description
    {
        get
        {
            var stringResource = new StringResource("DevHome.Settings/Resources");
            return stringResource.GetLocalized(Id + "_Description");
        }
    }

    [ObservableProperty]
    private bool isEnabled;

    public bool GetIsEnabled()
    {
        return LocalSettingsService!.ReadSettingAsync<bool>($"ExperimentalFeature_{Id}").Result;
    }

    partial void OnIsEnabledChanging(bool value)
    {
        LocalSettingsService!.SaveSettingAsync($"ExperimentalFeature_{Id}", value).Wait();
    }
}
