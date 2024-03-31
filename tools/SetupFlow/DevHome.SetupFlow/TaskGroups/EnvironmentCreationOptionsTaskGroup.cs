// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.ViewModels.Environments;

namespace DevHome.SetupFlow.TaskGroups;

public class EnvironmentCreationOptionsTaskGroup : ISetupTaskGroup, IRecipient<CreationAdaptiveCardSessionEndedMessage>
{
    private readonly SetupFlowViewModel _setupFlowViewModel;

    private readonly EnvironmentCreationOptionsViewModel _environmentCreationOptionsViewModel;

    private readonly CreateEnvironmentReviewViewModel _createEnvironmentReviewViewModel;

    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public EnvironmentCreationOptionsTaskGroup(
        EnvironmentCreationOptionsViewModel environmentCreationOptionsViewModel,
        CreateEnvironmentReviewViewModel createEnvironmentReviewViewModel,
        SetupFlowViewModel setupFlow)
    {
        _environmentCreationOptionsViewModel = environmentCreationOptionsViewModel;
        _createEnvironmentReviewViewModel = createEnvironmentReviewViewModel;

        // Register for the adaptive card session ended message so we can use the session data to create the environment
        WeakReferenceMessenger.Default.Register<CreationAdaptiveCardSessionEndedMessage>(this);
        _setupFlowViewModel = setupFlow;
        _setupFlowViewModel.EndSetupFlow += OnEndSetupFlow;
    }

    public IEnumerable<ISetupTask> SetupTasks => new List<ISetupTask>();

    public SetupPageViewModelBase GetSetupPageViewModel() => _environmentCreationOptionsViewModel;

    public ReviewTabViewModelBase GetReviewTabViewModel() => _createEnvironmentReviewViewModel;

    public void Receive(CreationAdaptiveCardSessionEndedMessage message)
    {
    }

    private void OnEndSetupFlow(object sender, EventArgs e)
    {
        WeakReferenceMessenger.Default.Unregister<CreationAdaptiveCardSessionEndedMessage>(this);
        _setupFlowViewModel.EndSetupFlow -= OnEndSetupFlow;
    }
}
