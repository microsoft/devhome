// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
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

    public SetupPageViewModelBase GetSetupPageViewModel() => _viewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel()
    {
        // Configuration file does not have a review tab
        return null;
    }
}
