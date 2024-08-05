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
        ExperimentalFeatures = experimentationService!.ExperimentalFeatures.Where(x => x.IsVisible && (!x.NeedsFeaturePresenceCheck || IsFeaturePresent(x))).OrderBy(x => x.Id).ToList();

        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), typeof(ExperimentalFeaturesViewModel).FullName!),
        };
    }

    /// <summary>
    /// Checks if the specified experimental feature is present on the machine.
    /// This method should be extended to handle new features by adding the corresponding
    /// feature check logic. If a feature is supported on the current machine, it should
    /// return false here.
    /// </summary>
    private bool IsFeaturePresent(ExperimentalFeature experimentalFeature)
    {
        if (string.Equals(experimentalFeature.Id, "QuietBackgroundProcessesExperiment", StringComparison.OrdinalIgnoreCase))
        {
            return QuietBackgroundProcessesSessionManager.IsFeaturePresent();
        }

        throw new NotImplementedException();
    }
}
