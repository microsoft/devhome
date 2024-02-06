// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.ViewModels;

namespace DevHome.SetupFlow.TaskGroups;

public class ConfigurationFileTaskGroup : ISetupTaskGroup
{
    private readonly ConfigurationFileViewModel _viewModel;

    public ConfigurationFileTaskGroup(ConfigurationFileViewModel configurationFileViewModel)
    {
        _viewModel = configurationFileViewModel;
    }

    public async Task<bool> PickConfigurationFileAsync() => await _viewModel.PickConfigurationFileAsync();

    public IEnumerable<ISetupTask> SetupTasks => _viewModel.TaskList;

    /// <summary>
    /// Gets the task corresponding to the configuration file to apply
    /// </summary>
    /// <remarks>At most one configuration file can be applied at a time</remarks>
    public ConfigureTask ConfigureTask => _viewModel.TaskList.FirstOrDefault();

    public SetupPageViewModelBase GetSetupPageViewModel() => _viewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel()
    {
        // Configuration file does not have a review tab
        return null;
    }
}
