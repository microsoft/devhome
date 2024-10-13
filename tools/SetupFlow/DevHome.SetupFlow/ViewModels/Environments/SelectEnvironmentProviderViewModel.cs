// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using CommunityToolkit.WinUI.Helpers;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using DevHome.Common.ViewModels;
using DevHome.SetupFlow.Models.Environments;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace DevHome.SetupFlow.ViewModels.Environments;

public partial class SelectEnvironmentProviderViewModel : SetupPageViewModelBase, IDisposable
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(SelectEnvironmentProviderViewModel));

    private readonly IComputeSystemService _computeSystemService;

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly SemaphoreSlim _extensionUpdateLock = new(1, 1);

    public ComputeSystemProviderDetails SelectedProvider { get; private set; }

    private EnvironmentsNotificationHelper _notificationsHelper;

    private bool _disposed;

    [ObservableProperty]
    private bool _areProvidersLoaded;

    [ObservableProperty]
    private int _selectedProviderIndex;

    [ObservableProperty]
    private bool _shouldShowCallToActionText;

    [ObservableProperty]
    private ObservableCollection<ComputeSystemProviderViewModel> _providersViewModels;

    public ExtensionInstallationExpanderViewModel InstallationViewModel { get; }

    public SelectEnvironmentProviderViewModel(
        ISetupFlowStringResource stringResource,
        SetupFlowOrchestrator orchestrator,
        IExtensionService extensionService,
        DispatcherQueue dispatcherQueue,
        ExtensionInstallationExpanderViewModel installationViewModel,
        IComputeSystemService computeSystemService)
           : base(stringResource, orchestrator)
    {
        PageTitle = stringResource.GetLocalized(StringResourceKey.SelectEnvironmentPageTitle);
        _computeSystemService = computeSystemService;
        InstallationViewModel = installationViewModel;
        InstallationViewModel.ExtensionChangedEvent += OnExtensionsChanged;
        _dispatcherQueue = dispatcherQueue;
    }

    public void OnExtensionsChanged(object sender, EventArgs args)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await LoadProvidersAsync();
        });
    }

    private async Task LoadProvidersAsync()
    {
        await _extensionUpdateLock.WaitAsync();

        try
        {
            CanGoToNextPage = false;
            AreProvidersLoaded = false;
            ShouldShowCallToActionText = false;
            Orchestrator.NotifyNavigationCanExecuteChanged();

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

            if (ProvidersViewModels.Count == 0)
            {
                ShouldShowCallToActionText = true;
            }

            AreProvidersLoaded = true;
        }
        finally
        {
            _extensionUpdateLock.Release();
        }
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

            // Using the default channel to send the message to the recipient. In this case, the EnvironmentCreationOptionsViewModel.
            // In the future if we support a multi-instance setup flow, we can use a custom channel/a message broker to send messages.
            // For now, we are using the default channel.
            WeakReferenceMessenger.Default.Send(new CreationProviderChangedMessage(SelectedProvider));
            CanGoToNextPage = true;
            Orchestrator.NotifyNavigationCanExecuteChanged();
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

    [RelayCommand]
    private async Task OnLoadedAsync(StackedNotificationsBehavior notificationQueue)
    {
        _notificationsHelper = new(notificationQueue);
        await LoadProvidersAsync();
        await InstallationViewModel.UpdateExtensionPackageInfoAsync();
    }

    [RelayCommand]
    private void OnUnLoaded()
    {
        _notificationsHelper = null;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _extensionUpdateLock.Dispose();
            }

            _disposed = true;
        }
    }
}
