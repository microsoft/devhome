// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;
using DevHome.Common.Contracts;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;

namespace DevHome.Services;

public class ExperimentationService : IExperimentationService
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly Microsoft.Internal.Windows.DevHome.Helpers.Experimentation.Experiment _experimentHelper = new();
    private bool _isExperimentationEnabled;

    public List<ExperimentalFeature> ExperimentalFeatures { get; } = new();

    public ExperimentationService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;

        _isExperimentationEnabled = true;
        if (_localSettingsService.HasSettingAsync("ExperimentationEnabled").Result)
        {
            _isExperimentationEnabled = _localSettingsService.ReadSettingAsync<bool>("ExperimentationEnabled").Result;
        }

        var isSeeker = _localSettingsService.ReadSettingAsync<bool>("IsSeeker").Result;
        TelemetryFactory.Get<ITelemetry>().Log("Seeker_Event", LogLevel.Critical, new SeekerEvent(isSeeker));
    }

    public bool IsFeatureEnabled(string key)
    {
        foreach (var experimentalFeature in ExperimentalFeatures)
        {
            if (experimentalFeature.Id == key)
            {
                return experimentalFeature.IsEnabled;
            }
        }

        return false;
    }

    public void AddExperimentalFeature(ExperimentalFeature experimentalFeature)
    {
        ExperimentalFeatures.Add(experimentalFeature);
    }

    public bool IsExperimentationEnabled
    {
        get => _isExperimentationEnabled;

        set
        {
            if (_isExperimentationEnabled != value)
            {
                _isExperimentationEnabled = value;

                Task.Run(() =>
                {
                    TelemetryFactory.Get<ITelemetry>().Log("Experimentation_Toggled_Event", LogLevel.Critical, new ExperimentationEvent(_isExperimentationEnabled));
                    _localSettingsService!.SaveSettingAsync($"IsSeeker", true);
                    return _localSettingsService!.SaveSettingAsync($"ExperimentationEnabled", _isExperimentationEnabled);
                }).Wait();
            }
        }
    }

    public bool IsExperimentEnabled(string key)
    {
        return IsExperimentationEnabled && _experimentHelper.IsEnabled(key);
    }
}
