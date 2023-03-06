// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Models;
using DevHome.SetupFlow.Common.ViewModels;
using DevHome.SetupFlow.ConfigurationFile.Models;
using DevHome.SetupFlow.ConfigurationFile.ViewModels;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.AppManagement;

public class ConfigurationFileTaskGroup : ISetupTaskGroup
{
    private readonly IList<ConfigureTask> _configurationTasks = new List<ConfigureTask>();
    private readonly IHost _host;
    private readonly ConfigurationFileViewModel _viewModel;

    public ConfigurationFileTaskGroup(IHost host)
    {
        _host = host;
        _viewModel = _host.GetService<ConfigurationFileViewModel>();
    }

    public async Task<bool> PickConfigurationFileAsync() => await _viewModel.PickConfigurationFileAsync();

    public IEnumerable<ISetupTask> SetupTasks => _configurationTasks;

    public SetupPageViewModelBase GetSetupPageViewModel() => _viewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => throw new System.NotImplementedException();
}
