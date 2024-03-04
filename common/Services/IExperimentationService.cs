// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Models;

namespace DevHome.Common.Services;

public interface IExperimentationService
{
    List<ExperimentalFeature> ExperimentalFeatures { get; }

    bool IsFeatureEnabled(string key);

    void AddExperimentalFeature(ExperimentalFeature experimentalFeature);

    bool IsExperimentEnabled(string key);
}
