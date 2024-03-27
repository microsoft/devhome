// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Dashboard.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Dashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public IWidgetHostingService WidgetHostingService { get; }

    public IWidgetIconService WidgetIconService { get; }

    public IWidgetScreenshotService WidgetScreenshotService { get; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasWidgetService;

    public DashboardViewModel(
        IWidgetHostingService widgetHostingService,
        IWidgetIconService widgetIconService,
        IWidgetScreenshotService widgetScreenshotService)
    {
        WidgetHostingService = widgetHostingService;
        WidgetIconService = widgetIconService;
        WidgetScreenshotService = widgetScreenshotService;
    }

    public Visibility GetNoWidgetMessageVisibility(int widgetCount, bool isLoading)
    {
        return (widgetCount == 0 && !isLoading && HasWidgetService) ? Visibility.Visible : Visibility.Collapsed;
    }
}
