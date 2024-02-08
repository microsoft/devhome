﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Services;

namespace DevHome.Common.Models;

public partial class ExperimentalFeature : ObservableObject
{
    private readonly bool _isEnabledByDefault;

    public string Id { get; init; }

    public bool IsVisible { get; init; }

    public static ILocalSettingsService? LocalSettingsService { get; set; }

    public static IExperimentationService? ExperimentationService { get; set; }

    public ExperimentalFeature(string id, bool enabledByDefault, bool visible = true)
    {
        Id = id;
        _isEnabledByDefault = enabledByDefault;
        IsVisible = visible;

        IsEnabled = CalculateEnabled();
    }

    private bool CalculateEnabled()
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
        return ExperimentationService!.IsEnabled(Id);
    }

    partial void OnIsEnabledChanging(bool value)
    {
        return;
    }

    [RelayCommand]
    public async Task OnToggledAsync()
    {
        ExperimentationService!.SetIsEnabled(Id, !IsEnabled);

        await Task.CompletedTask;
    }
}
