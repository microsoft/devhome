// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.DevInsights.Services;
using Microsoft.UI.Xaml;

namespace DevHome.DevInsights.ViewModels;

public partial class InsightsPageViewModel : ObservableObject
{
    [ObservableProperty]
    private DIInsightsService _insightsService;

    public InsightsPageViewModel()
    {
        _insightsService = Application.Current.GetService<DIInsightsService>();
    }
}
