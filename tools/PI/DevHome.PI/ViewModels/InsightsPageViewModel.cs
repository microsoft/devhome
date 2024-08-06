// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.PI.Services;
using Microsoft.UI.Xaml;

namespace DevHome.PI.ViewModels;

public partial class InsightsPageViewModel : ObservableObject
{
    [ObservableProperty]
    private PIInsightsService _insightsService;

    public InsightsPageViewModel()
    {
        _insightsService = Application.Current.GetService<PIInsightsService>();
    }
}
