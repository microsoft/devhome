// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// The view model for the list of compute systems retrieved from a single compute system provider.
/// </summary>
public partial class ComputeSystemsListViewModel : ObservableObject
{
    private const string SortByComputeSystemTitle = "ComputeSystemTitle";

    [NotifyPropertyChangedFor(nameof(FormattedDeveloperId))]
    [ObservableProperty]
    private string _displayName;

    public event EventHandler<ComputeSystem> CardSelectionChanged = (s, e) => { };

    [ObservableProperty]
    private object _selectedItem;

    public ObservableCollection<ComputeSystemCardViewModel> ComputeSystemCardCollection { get; private set; } = new();

    public AdvancedCollectionView ComputeSystemCardAdvancedCollectionView { get; private set; }

    public List<DeveloperIdWrapper> DeveloperIdList { get; set; }

    public List<ComputeSystemsResult> ComputeSystemsResults { get; set; }

    public ComputeSystemProvider Provider { get; set; }

    public DeveloperIdWrapper CurrentDeveloperId { get; set; }

    public ComputeSystemsResult CurrentResult { get; set; }

    public List<ComputeSystem> ComputeSystemWrappers { get; set; } = new();

    /// <summary>
    /// Gets the Formatted the developerId login string that will be displayed in the UI.
    /// </summary>
    /// <returns>The formatted DeveloperId which contains the loginId wrapped in parentheses</returns>
    public string FormattedDeveloperId
    {
        get
        {
            if (CurrentDeveloperId == null || string.IsNullOrEmpty(CurrentDeveloperId.LoginId))
            {
                return string.Empty;
            }

            return "(" + CurrentDeveloperId.LoginId + ")";
        }
    }

    /// <summary>
    /// Gets the error text that will appear in the UI.
    /// </summary>
    public string ErrorText
    {
        get
        {
            var result = CurrentResult.Result;
            if (result?.Status == ProviderOperationStatus.Failure)
            {
                Log.Logger.ReportError(Log.Component.ComputeSystemsListViewModel, $"Failed to get Compute system due to error. Display: {result.DisplayMessage}, DiagnosticText: {result.DiagnosticText}, ExtendedError: {result.ExtendedError}");
                return result.DisplayMessage ?? result.DiagnosticText;
            }

            return string.Empty;
        }
    }

    public ComputeSystemsListViewModel(ComputeSystemsLoadedData loadedData)
    {
        Provider = loadedData.ProviderToDevIdMap.Key;
        ComputeSystemsResults = loadedData.ComputeSystemsResult;
        CurrentResult = ComputeSystemsResults.FirstOrDefault();
        DeveloperIdList = loadedData.ProviderToDevIdMap.Value;
        CurrentDeveloperId = DeveloperIdList.FirstOrDefault();

        DisplayName = Provider.DisplayName;

        if (CurrentResult != null && CurrentResult.ComputeSystems != null)
        {
            CurrentResult = ComputeSystemsResults.FirstOrDefault();
            ComputeSystemWrappers = CurrentResult.ComputeSystems.Select(computeSystem => new ComputeSystem(computeSystem)).ToList();
        }

        // Create a new AdvancedCollectionView for the ComputeSystemCards collection.
        ComputeSystemCardAdvancedCollectionView = new(ComputeSystemCardCollection);

        // Always Sort the cards by the compute system title.
        ComputeSystemCardAdvancedCollectionView.SortDescriptions.Add(new SortDescription(SortByComputeSystemTitle, SortDirection.Ascending));
    }

    /// <summary>
    /// Filter the cards based on the text entered in the search box. Cards will be filtered by the ComputeSystemTitle.
    /// </summary>
    /// <param name="text">Text the user enters into the textbox</param>
    public void FilterComputeSystemCards(string text)
    {
        ComputeSystemCardAdvancedCollectionView.Filter = item =>
        {
            try
            {
                if (item is ComputeSystemCardViewModel card)
                {
                    return string.IsNullOrEmpty(text) || card.ComputeSystemTitle.Contains(text, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.ReportError(Log.Component.ComputeSystemsListViewModel, $"Failed to filter Compute system cards. Error: {ex.Message}");
            }

            return true;
        };

        ComputeSystemCardAdvancedCollectionView.RefreshFilter();
    }

    /// <summary>
    /// Update subscriber with the compute system wrapper that is currently selected in the UI.
    /// </summary>
    /// <param name="viewModel">Environments card selected by the user.</param>
    [RelayCommand]
    public void ContainerSelectionChanged(ComputeSystemCardViewModel viewModel)
    {
        if (viewModel == null)
        {
            return;
        }

        SelectedItem = viewModel;
        CardSelectionChanged(this, viewModel.ComputeSystemWrapper);
    }
}
