// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public IWidgetHostingService WidgetHostingService { get; }

    public IWidgetIconService WidgetIconService { get; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasWidgetService;

    public DashboardViewModel(
        IWidgetHostingService widgetHostingService,
        IWidgetIconService widgetIconService)
    {
        WidgetIconService = widgetIconService;
        WidgetHostingService = widgetHostingService;
    }

    public Visibility GetNoWidgetMessageVisibility(int widgetCount, bool isLoading)
    {
        if (widgetCount == 0 && !isLoading && HasWidgetService)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }
}
