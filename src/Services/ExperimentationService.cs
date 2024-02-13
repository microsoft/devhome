// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Contracts;
using DevHome.Common.Models;
using DevHome.Common.Services;

namespace DevHome.Services;

public class ExperimentationService : IExperimentationService
{
    private readonly ILocalSettingsService _localSettingsService;

    public List<ExperimentalFeature> ExperimentalFeatures { get; } = new();

    public ExperimentationService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
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
}
