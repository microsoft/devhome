// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.Internal.Windows.DevHome.Helpers.Experimentation;

namespace DevHome.Services;

public class ExperimentationService : IExperimentationService
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly Experiment _experimentHelper = new();

    public List<ExperimentalFeature> ExperimentalFeatures { get; } = new();

    public ExperimentationService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;

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

    public bool IsExperimentEnabled(string key)
    {
        return _experimentHelper.IsEnabled(key);
    }
}
