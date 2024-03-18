// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Environments.Helpers;
using DevHome.Common.Environments.Models;
using DevHome.Common.Environments.Services;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Environments.Helpers;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// The main view model for the landing page of the Environments tool.
/// </summary>
public partial class LandingPageViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private readonly EnvironmentsExtensionsService _extensionsService;

    private readonly ToastNotificationService _notificationService;

    private readonly IComputeSystemManager _computeSystemManager;

    private readonly object _lock = new();

    public bool IsLoading { get; set; }

    public ObservableCollection<ComputeSystemViewModel> ComputeSystems { get; set; } = new();

    public AdvancedCollectionView ComputeSystemsView { get; set; }

    public bool HasPageLoadedForTheFirstTime { get; set; }

    [ObservableProperty]
    private bool _showLoadingShimmer = true;

    public LandingPageViewModel(IComputeSystemManager manager, EnvironmentsExtensionsService extensionsService, ToastNotificationService toastNotificationService)
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        _extensionsService = extensionsService;
        _notificationService = toastNotificationService;
        _computeSystemManager = manager;

        ComputeSystemsView = new AdvancedCollectionView(ComputeSystems);
    }

    [RelayCommand]
    public async Task SyncButton()
    {
        // temporary, we'll need to give the users a way to disable this.
        // if they don't want to use hyper-v
        _notificationService.CheckIfUserIsAHyperVAdmin();
        await LoadModelAsync();
    }

    /// <summary>
    /// Main entry point for loading the view model.
    /// </summary>
    public async Task LoadModelAsync(bool useDebugValues = false)
    {
        lock (_lock)
        {
            if (IsLoading)
            {
                return;
            }

            HasPageLoadedForTheFirstTime = true;
            IsLoading = true;
        }

        // temporary, we'll need to give the users a way to disable this.
        // if they don't want to use hyper-v
        _notificationService.CheckIfUserIsAHyperVAdmin();
        for (var i = ComputeSystems.Count - 1; i >= 0; i--)
        {
            ComputeSystems[i].RemoveStateChangedHandler();
            ComputeSystems.RemoveAt(i);
        }

        ShowLoadingShimmer = true;
        await _extensionsService.GetComputeSystemsAsync(useDebugValues, AddAllComputeSystemsFromAProvider);
        ShowLoadingShimmer = false;

        lock (_lock)
        {
            IsLoading = false;
        }
    }

    private async Task AddAllComputeSystemsFromAProvider(ComputeSystemsLoadedData data)
    {
        var provider = data.ProviderDetails.ComputeSystemProvider;

        await _dispatcher.EnqueueAsync(async () =>
        {
            try
            {
                var computeSystemList = data.DevIdToComputeSystemMap.Values.SelectMany(x => x.ComputeSystems).ToList();

                // In the future when we support switching between accounts in the environments page, we will need to handle this differently.
                // for now we'll show all the compute systems from a provider.
                var computeSystemResult = data.DevIdToComputeSystemMap.Values.FirstOrDefault();

                if (computeSystemList == null || computeSystemList.Count == 0)
                {
                    Log.Logger()?.ReportError($"No Compute systems found for provider: {provider.Id}");
                    return;
                }

                for (var i = 0; i < computeSystemList.Count; i++)
                {
                    var packageFullName = data.ProviderDetails.ExtensionWrapper.PackageFullName;
                    var computeSystemViewModel = new ComputeSystemViewModel(_computeSystemManager, computeSystemList.ElementAt(i), provider, packageFullName);
                    await computeSystemViewModel.InitializeCardDataAsync();
                    ComputeSystems.Add(computeSystemViewModel);
                }
            }
            catch (Exception ex)
            {
                Log.Logger()?.ReportError($"Error occurred while adding Compute systems to environments page for provider: {provider.Id}", ex);
            }
        });
    }

    /// <summary>
    /// Updates the view model to show only the compute systems that match the search criteria.
    /// </summary>
    [RelayCommand]
    public void SearchHandler(string query)
    {
        ComputeSystemsView.Filter = system =>
        {
            if (system is ComputeSystemViewModel computeSystemViewModel)
            {
                var systemName = computeSystemViewModel.ComputeSystem.DisplayName;
                var systemAltName = computeSystemViewModel.ComputeSystem.SupplementalDisplayName;
                return systemName.Contains(query, StringComparison.OrdinalIgnoreCase) || systemAltName.Contains(query, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        };
    }

    /// <summary>
    /// Updates the view model to sort the compute systems according to the sort criteria.
    /// </summary>
    [RelayCommand]
    public void SortHandler(string critieria)
    {
        ComputeSystemsView.SortDescriptions.Clear();
        if (critieria == "Name")
        {
            ComputeSystemsView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
            return;
        }
        else if (critieria == "Alternative Name")
        {
            ComputeSystemsView.SortDescriptions.Add(new SortDescription("AlternativeName", SortDirection.Ascending));
            return;
        }
    }
}
