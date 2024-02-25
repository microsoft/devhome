// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Views;
using Microsoft.Extensions.Hosting;

namespace DevHome.SetupFlow.TaskGroups;

public class SetupTargetTaskGroup : ISetupTaskGroup, IDisposable
{
    private readonly SetupTargetViewModel _setupTargetViewModel;
    private readonly SetupTargetReviewViewModel _setupTargetReviewViewModel;

    private readonly ConfigureTargetTask _setupTargetTaskGroup;

    private bool _disposedValue;

    public SetupTargetTaskGroup(
        SetupTargetViewModel setupTargetViewModel,
        SetupTargetReviewViewModel setupTargetReviewViewModel,
        ISetupFlowStringResource stringResource,
        IComputeSystemManager computeSystemManager,
        ConfigurationFileBuilder configurationFileBuilder,
        SetupFlowOrchestrator setupFlowOrchestrator,
        IThemeSelectorService themeSelectorService)
    {
        _setupTargetViewModel = setupTargetViewModel;
        _setupTargetReviewViewModel = setupTargetReviewViewModel;

        _setupTargetTaskGroup = new ConfigureTargetTask(
            stringResource,
            computeSystemManager,
            configurationFileBuilder,
            setupFlowOrchestrator,
            themeSelectorService);
    }

    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>() { _setupTargetTaskGroup };

    public SetupPageViewModelBase GetSetupPageViewModel() => _setupTargetViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _setupTargetReviewViewModel;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _setupTargetTaskGroup.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
