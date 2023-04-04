// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.SetupFlow.Common.ViewModels;

/// <summary>
/// Base view model class for all the tabs we show in the Review page of the Setup flow.
/// </summary>
public partial class ReviewTabViewModelBase : ObservableObject
{
    /// <summary>
    /// Title shown on the tabs bar.
    /// </summary>
    [ObservableProperty]
    private string _tabTitle = string.Empty;
}
