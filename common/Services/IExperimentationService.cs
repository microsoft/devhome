// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Models;

namespace DevHome.Common.Services;

public interface IExperimentationService
{
    bool IsFeatureEnabled(string key);

    List<ExperimentalFeature> ExperimentalFeatures { get; }

    void AddExperimentalFeature(ExperimentalFeature experimentalFeature);
}
