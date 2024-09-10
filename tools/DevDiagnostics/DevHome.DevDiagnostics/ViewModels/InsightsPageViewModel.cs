// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Services;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class InsightsPageViewModel : ObservableObject
{
    [ObservableProperty]
    private DDInsightsService _insightsService;

    public InsightsPageViewModel()
    {
        _insightsService = Application.Current.GetService<DDInsightsService>();
    }
}
