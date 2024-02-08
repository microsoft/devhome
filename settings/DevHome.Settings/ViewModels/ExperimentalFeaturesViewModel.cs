// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.Common.Services;

namespace DevHome.Settings.ViewModels;

public class ExperimentalFeaturesViewModel : ObservableObject
{
    public List<ExperimentalFeature> Features { get; } = new();

    public ExperimentalFeaturesViewModel(IExperimentationService experimentationService)
    {
        Features = experimentationService!.Features.OrderBy(x => x.Id).ToList();
    }
}
