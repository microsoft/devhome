// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.QuietBackgroundProcesses;

namespace DevHome.Settings.ViewModels;

public partial class ExperimentalFeaturesViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public List<ExperimentalFeature> ExperimentalFeatures { get; } = new();

    public ExperimentalFeaturesViewModel(IExperimentationService experimentationService)
    {
        ExperimentalFeatures = experimentationService!.ExperimentalFeatures.Where(x => x.IsVisible && (!x.IsFeaturePresentCheck || IsFeaturePresent(x))).OrderBy(x => x.Id).ToList();

        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), typeof(ExperimentalFeaturesViewModel).FullName!),
        };
    }

    // A crude function to check for feature presence
    private bool IsFeaturePresent(ExperimentalFeature experimentalFeature)
    {
        if (experimentalFeature.Id == "QuietBackgroundProcessesExperiment")
        {
            return QuietBackgroundProcessesSessionManager.IsFeaturePresent();
        }

        throw new NotImplementedException();
    }
}
