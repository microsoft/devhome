// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents;
using DevHome.Telemetry;

namespace DevHome.Services;

public class ExperimentationService : IExperimentationService
{
    private readonly ILocalSettingsService _localSettingsService;

    public List<ExperimentalFeature> Features { get; } = new();

    public ExperimentationService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public bool IsEnabled(string key)
    {
        if (_localSettingsService.HasSettingAsync($"ExperimentalFeature_{key}").Result)
        {
            return _localSettingsService.ReadSettingAsync<bool>($"ExperimentalFeature_{key}").Result;
        }

        foreach (var feature in Features)
        {
            if (feature.Id == key)
            {
                return feature.IsEnabled;
            }
        }

        return false;
    }

    public void SetIsEnabled(string key, bool value)
    {
        TelemetryFactory.Get<ITelemetry>().Log("RepoTool_SearchForExtensions_Event", LogLevel.Critical, new ExperimentalFeatureEvent(key, value));

        _localSettingsService.SaveSettingAsync($"ExperimentalFeature_{key}", value).Wait();
    }

    public void AddExperimentalFeature(ExperimentalFeature experimentalFeature)
    {
        Features.Add(experimentalFeature);
    }
}
