// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Models;

namespace DevHome.Common.Services;

public interface IExperimentationService
{
    bool IsEnabled(string key);

    void SetIsEnabled(string key, bool value);

    List<ExperimentalFeature> Features { get; }

    void AddExperimentalFeature(ExperimentalFeature experimentalFeature);
}
