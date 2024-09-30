// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.QuietBackgroundProcesses;
using Microsoft.Internal.Windows.DevHome.Helpers.FileExplorer;

namespace DevHome.Settings.ViewModels;

public partial class ExperimentalFeaturesViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public List<ExperimentalFeature> ExperimentalFeatures { get; } = new();

    private readonly IInfoBarService _infoBarService;

    private readonly StringResource _stringResource = new("DevHome.Settings.pri", "DevHome.Settings/Resources");

    [ObservableProperty]
    private bool _isExperimentalFeaturesGPOEnabled;

    public ExperimentalFeaturesViewModel(IExperimentationService experimentationService, IInfoBarService infoBarService)
    {
        ExperimentalFeatures = experimentationService!.ExperimentalFeatures.Where(x => x.IsVisible && (!x.NeedsFeaturePresenceCheck || IsFeaturePresent(x))).OrderBy(x => x.Id).ToList();
        _infoBarService = infoBarService;

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(_stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(_stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), typeof(ExperimentalFeaturesViewModel).FullName!),
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
            try
            {
                return QuietBackgroundProcessesSessionManager.IsFeaturePresent();
            }
            catch (Exception)
            {
                return false;
            }
        }

        if (string.Equals(experimentalFeature.Id, "FileExplorerSourceControlIntegration", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return ExtraFolderPropertiesWrapper.IsSupported();
            }
            catch (Exception)
            {
                return false;
            }
        }

        throw new NotImplementedException();
    }

    [RelayCommand]
    private void OnLoaded()
    {
        IsExperimentalFeaturesGPOEnabled = GPOHelper.GetConfiguredEnabledExperimentalFeaturesValue();
        if (!IsExperimentalFeaturesGPOEnabled)
        {
            // Turn off all experimental features
            foreach (var feature in ExperimentalFeatures)
            {
                feature.IsEnabled = false;
            }

            _infoBarService.ShowAppLevelInfoBar(
                Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning,
                _stringResource.GetLocalized("DevHomeFeatureIsBlocked"),
                string.Empty,
                false,
                IInfoBarService.PageScope.ExperimentalFeatures);
        }
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        if (_infoBarService.IsAppLevelInfoBarVisible() && _infoBarService.GetInfoBarPageScope() == IInfoBarService.PageScope.ExperimentalFeatures)
        {
            _infoBarService.HideAppLevelInfoBar();
        }
    }
}
