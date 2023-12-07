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

    private bool _validatedWebExpPack;

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel(
        IWidgetHostingService widgetHostingService,
        IWidgetIconService widgetIconService)
    {
        WidgetIconService = widgetIconService;
        WidgetHostingService = widgetHostingService;
    }

    public bool EnsureWebExperiencePack()
    {
        // If already validated there's a good version, don't check again.
        if (_validatedWebExpPack)
        {
            return true;
        }

        _validatedWebExpPack = WidgetHostingService.HasValidWebExperiencePack();
        return _validatedWebExpPack;
    }

    public Visibility GetNoWidgetMessageVisibility(int widgetCount, bool isLoading)
    {
        if (widgetCount == 0 && !isLoading)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }
}
