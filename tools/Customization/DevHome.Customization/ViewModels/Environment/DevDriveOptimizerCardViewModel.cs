// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Services;

namespace DevHome.Customization.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a dev drive optimizer on the dev drive insights page.
/// </summary>
public partial class DevDriveOptimizerCardViewModel : ObservableObject
{
    public string CacheToBeMoved { get; set; }

    public string DevDriveOptimizationSuggestion { get; set; }

    public string ExistingCacheLocation { get; set; }

    public string ExampleLocationOnDevDrive { get; set; }

    public string EnvironmentVariableToBeSet { get; set; }

    public string OptimizerDevDriveDescription { get; set; }

    public DevDriveOptimizerCardViewModel(string cacheToBeMoved, string existingCacheLocation, string exampleLocationOnDevDrive, string environmentVariableToBeSet)
    {
        CacheToBeMoved = cacheToBeMoved;
        ExistingCacheLocation = existingCacheLocation;
        ExampleLocationOnDevDrive = exampleLocationOnDevDrive;
        EnvironmentVariableToBeSet = environmentVariableToBeSet;
        var stringResource = new StringResource("DevHome.Customization/Resources");
        OptimizerDevDriveDescription = stringResource.GetLocalized("OptimizerDevDriveDescription", EnvironmentVariableToBeSet, ExistingCacheLocation, ExampleLocationOnDevDrive, EnvironmentVariableToBeSet);
        DevDriveOptimizationSuggestion = stringResource.GetLocalized("DevDriveOptimizationSuggestion");
    }
}
