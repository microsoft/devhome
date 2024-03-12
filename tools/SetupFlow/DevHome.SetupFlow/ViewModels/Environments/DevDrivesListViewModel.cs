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
using Microsoft.VisualBasic;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// The view model for the list of dev drives retrieved from a single dev drive provider.
/// </summary>
public partial class DevDrivesListViewModel : ObservableObject
{
    private const string SortByDevDriveTitle = "DevDriveTitle";

    private const string HyperVExtensionProviderName = "Microsoft.HyperV";

    [ObservableProperty]
    private bool _isHyperVExtension;

    [NotifyPropertyChangedFor(nameof(FormattedDeveloperId))]
    [ObservableProperty]
    private string _displayName;

    public event EventHandler<DevDrive> CardSelectionChanged = (s, e) => { };

    [ObservableProperty]
    private object _selectedItem;

    public ObservableCollection<DevDriveCardViewModel> DevDriveCardCollection { get; private set; } = new();

    public AdvancedCollectionView DevDriveCardAdvancedCollectionView { get; private set; }

    public DeveloperIdWrapper CurrentDeveloperId { get; set; }

    [ObservableProperty]
    private string _accessibilityName;

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
    public string ErrorText => string.Empty;

    public DevDrivesListViewModel(/*IEnumerable<IDevDrive> existingDevDrives*/)
    {
        // Create a new AdvancedCollectionView for the DevDriveCards collection.
        DevDriveCardAdvancedCollectionView = new(DevDriveCardCollection);
    }

    /// <summary>
    /// Filter the cards based on the text entered in the search box. Cards will be filtered by the DevDriveTitle.
    /// </summary>
    /// <param name="text">Text the user enters into the textbox</param>
    public void FilterDevDriveCards(string text)
    {
        DevDriveCardAdvancedCollectionView.Filter = item =>
        {
            try
            {
                if (item is DevDriveCardViewModel card)
                {
                    return string.IsNullOrEmpty(text) || card.DevDriveTitle.Contains(text, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }
            catch (Exception /*ex*/)
            {
                // Log.Logger.ReportError(Log.Component.DevDrivesListViewModel, $"Failed to filter Compute system cards. Error: {ex.Message}");
            }

            return true;
        };

        DevDriveCardAdvancedCollectionView.RefreshFilter();
    }

    /// <summary>
    /// Update subscriber with the dev drive wrapper that is currently selected in the UI.
    /// </summary>
    /// <param name="viewModel">Environments card selected by the user.</param>
    [RelayCommand]
    public void ContainerSelectionChanged(DevDriveCardViewModel viewModel)
    {
        if (viewModel == null)
        {
            return;
        }

        SelectedItem = viewModel;
        CardSelectionChanged(this, viewModel.DevDriveWrapper);
    }
}
