// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Services;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using WinUIEx;

namespace DevHome.SetupFlow.ViewModels.Environments;

public partial class SelectEnvironmentProviderViewModel : SetupPageViewModelBase
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SelectEnvironmentProviderViewModel));

    private readonly IComputeSystemService _computeSystemService;

    public ComputeSystemProviderDetails SelectedProvider { get; private set; }

    private EnvironmentsNotificationHelper _notificationsHelper;

    private bool _isFirstTimeLoading;

    [ObservableProperty]
    private string _callToActionText;

    [ObservableProperty]
    private string _callToActionHyperLinkButtonText;

    [ObservableProperty]
    private bool _areProvidersLoaded;

    [ObservableProperty]
    private int _selectedProviderIndex;

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
        _isFirstTimeLoading = true;
    }

    private async Task LoadProvidersAsync()
    {
        CanGoToNextPage = false;
        AreProvidersLoaded = false;
        Orchestrator.NotifyNavigationCanExecuteChanged();
        CallToActionText = null;
        CallToActionHyperLinkButtonText = null;

        var providerDetails = await Task.Run(_computeSystemService.GetComputeSystemProvidersAsync);
        ProvidersViewModels = new();
        foreach (var providerDetail in providerDetails)
        {
            // Only list providers that support creation
            if (providerDetail.ComputeSystemProvider.SupportedOperations.HasFlag(ComputeSystemProviderOperations.CreateComputeSystem))
            {
                _notificationsHelper?.DisplayComputeSystemProviderErrors(providerDetail);
                ProvidersViewModels.Add(new ComputeSystemProviderViewModel(providerDetail));
            }
        }

        AreProvidersLoaded = true;

        var callToActionData = ComputeSystemHelpers.UpdateCallToActionText(ProvidersViewModels.Count, true);
        CallToActionText = callToActionData.CallToActionText;
        CallToActionHyperLinkButtonText = callToActionData.CallToActionHyperLinkText;
    }

    [RelayCommand]
    private void EnableNextButton(ComputeSystemProviderViewModel sender)
    {
        // Using the default channel to send the message to the recipient. In this case, the EnvironmentCreationOptionsViewModel.
        // In the future if we support a multi-instance setup flow, we can use a custom channel/a message broker to send messages.
        // For now, we are using the default channel.
        WeakReferenceMessenger.Default.Send(new CreationProviderChangedMessage(SelectedProvider));
        CanGoToNextPage = true;
        Orchestrator.NotifyNavigationCanExecuteChanged();
    }

    [RelayCommand]
    private void ItemsViewSelectionChanged(ComputeSystemProviderViewModel sender)
    {
        if (sender != null)
        {
            // When navigating between the select providers page and the configure creation options page
            // visual selection is lost, so we need deselect the providers first. Then select correct one.
            // this will ensure that the correct provider is visually selected when navigating back to the select providers page.
            foreach (var provider in ProvidersViewModels)
            {
                provider.IsSelected = false;
            }

            sender.IsSelected = true;
            SelectedProvider = sender.ProviderDetails;
        }
    }

    public async Task InitializeAsync(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = new(notificationQueue);

        if (_isFirstTimeLoading || !string.IsNullOrEmpty(CallToActionText))
        {
            _isFirstTimeLoading = false;
            CanGoToNextPage = false;
            await LoadProvidersAsync();
        }
    }

    /// <summary>
    /// Navigates the user to the extensions page library
    /// process.
    /// </summary>
    [RelayCommand]
    public void CallToActionButton()
    {
        Orchestrator.NavigateToOutsideFlow(KnownPageKeys.Extensions);
    }
}
