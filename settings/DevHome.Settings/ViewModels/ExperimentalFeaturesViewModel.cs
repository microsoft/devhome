// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Settings.Models;

namespace DevHome.Settings.ViewModels;

public class ExperimentalFeaturesViewModel : BreadcrumbViewModel
{
    public List<ExperimentalFeature> ExperimentalFeatures { get; } = new();

    public override ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ExperimentalFeaturesViewModel(IExperimentationService experimentationService)
    {
        ExperimentalFeatures = experimentationService!.ExperimentalFeatures.OrderBy(x => x.Id).ToList();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), typeof(ExperimentalFeaturesViewModel).FullName!),
        };
    }
}
