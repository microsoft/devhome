// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;

namespace DevHome.SetupFlow.ViewModels.Environments;

public partial class SelectEnvironmentProviderViewModel : SetupPageViewModelBase
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly IComputeSystemService _computeSystemService;

    public ComputeSystemProviderDetails SelectedProvider { get; private set; }

    [ObservableProperty]
    private ObservableCollection<ComputeSystemProviderViewModel> _providersViewModels;

    public SelectEnvironmentProviderViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IComputeSystemService computeSystemService)
           : base(stringResource, orchestrator)
    {
        PageTitle = stringResource.GetLocalized(StringResourceKey.SelectEnvironmentPageTitle);
        _computeSystemService = computeSystemService;
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    }

    public void LoadProviders()
    {
        Task.Run(() =>
        {
            _dispatcher.TryEnqueue(async () =>
            {
                var providerDetails = await _computeSystemService.GetComputeSystemProvidersAsync();
                ProvidersViewModels = new();

                foreach (var providerDetail in providerDetails)
                {
                    ProvidersViewModels.Add(new ComputeSystemProviderViewModel(providerDetail));
                }
            });
        });
    }

    protected async override Task OnFirstNavigateToAsync()
    {
        CanGoToNextPage = false;
        LoadProviders();

        // Does nothing, but we need to override this as the base expects a task to be returned.
        await Task.CompletedTask;
    }

    [RelayCommand]
    public void ItemsViewSelectionChanged(ComputeSystemProviderViewModel sender)
    {
        if (sender != null)
        {
            SelectedProvider = sender.ProviderDetails;

            // Using the default channel to send the message to the recipient. In this case, the EnvironmentCreationOptionsViewModel.
            // In the future if we support a multi-instance setup flow, we can use a custom channel/a message broker to send messages.
            // For now, we are using the default channel.
            WeakReferenceMessenger.Default.Send(new CreationProviderChangedMessage(new CreationProviderChangedData(SelectedProvider, new AdaptiveCardRenderer())));
            CanGoToNextPage = true;
        }
    }
}
