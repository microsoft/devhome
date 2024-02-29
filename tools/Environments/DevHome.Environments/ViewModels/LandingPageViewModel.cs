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
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Environments.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.DevHome.SDK;
using WinUIEx;

namespace DevHome.Environments.ViewModels;

/// <summary>
/// The main view model for the landing page of the Environments tool.
/// </summary>
public partial class LandingPageViewModel : ObservableObject
{
    private readonly EnvironmentsExtensionsService _extensionsService;

    public ObservableCollection<ComputeSystemViewModel> ComputeSystems { get; set; } = new();

    public AdvancedCollectionView ComputeSystemsView { get; set; }

    [ObservableProperty]
    private bool _showLoadingShimmer = true;

    public LandingPageViewModel(EnvironmentsExtensionsService extensionsService)
    {
        _extensionsService = extensionsService;

        // ToDo: Re-enable in production
        // LoadModel();
        ComputeSystemsView = new AdvancedCollectionView(ComputeSystems);
    }

    // ToDo: Sync button should clear the view model and reload it
    public void SyncButton_Click(object sender, RoutedEventArgs e)
    {
        ComputeSystems.Clear();
        LoadModel();
    }

    /// <summary>
    /// Main entry point for loading the view model.
    /// </summary>
    public async void LoadModel(bool useDebugValues = false)
    {
        ShowLoadingShimmer = true;
        foreach (var system in await _extensionsService.GetComputeSystemsAsync(useDebugValues))
        {
            ComputeSystems.Add(system);
            SubscribeForStateChanges(system);
        }

        ShowLoadingShimmer = false;
    }

    private void SubscribeForStateChanges(ComputeSystemViewModel system)
    {
        system.ComputeSystem.StateChanged += (sender, args) =>
        {
            // ToDo: Is this the best way to update the UI?
            Application.Current.GetService<WindowEx>().DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    system.State = args;
                    var index = ComputeSystems.IndexOf(system);
                    ComputeSystems[index] = system;
                }
                catch (Exception e)
                {
                    var err = e.Message;

                    // ToDo: Remove this and add logging
                    Debug.WriteLine(err);
                }
            });
        };
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
