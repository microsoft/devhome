// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.ViewModels;

/// <summary>
/// Base view model class for all the tabs we show in the Review page of the Setup flow.
/// </summary>
public abstract partial class ReviewTabViewModelBase : ObservableObject
{
    /// <summary>
    /// Title shown on the tabs bar.
    /// </summary>
    [ObservableProperty]
    private string _tabTitle = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the tab has any items to display
    /// </summary>
    public abstract bool HasItems { get; }
}
