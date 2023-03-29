// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.ConfigurationFile.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.ConfigurationFile;

public class ConfigurationFileTaskGroup : ISetupTaskGroup
{
    private readonly IHost _host;
    private readonly ConfigurationFileViewModel _viewModel;

    public bool RequiresReview => false;

    public ConfigurationFileTaskGroup(IHost host)
    {
        _host = host;
        _viewModel = _host.GetService<ConfigurationFileViewModel>();
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
