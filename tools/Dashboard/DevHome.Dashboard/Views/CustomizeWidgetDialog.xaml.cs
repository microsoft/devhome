// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;

namespace DevHome.Dashboard.Views;

public sealed partial class CustomizeWidgetDialog : ContentDialog
{
    public WidgetViewModel ViewModel { get; set; }

    private readonly IWidgetHostingService _hostingService;

    private readonly Widget _widget;
    private readonly WidgetDefinition _widgetDefinition;
    private static DispatcherQueue _dispatcher;

    public CustomizeWidgetDialog(Widget widget, DispatcherQueue dispatcher, WidgetDefinition widgetDefinition)
    {
        var size = WidgetHelpers.GetLargestCapabilitySize(widgetDefinition.GetWidgetCapabilities());

        ViewModel = new WidgetViewModel(null, size, _widgetDefinition, dispatcher)
        {
            IsInEditMode = true,
        };
        _hostingService = Application.Current.GetService<IWidgetHostingService>();

        this.InitializeComponent();

        _widget = widget;
        _widgetDefinition = widgetDefinition;
        _dispatcher = dispatcher;

        _hostingService.GetWidgetCatalog()!.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;

        this.Loaded += InitializeWidgetCustomization;
    }

    private async void InitializeWidgetCustomization(object sender, RoutedEventArgs e)
    {
        ViewModel.Widget = _widget;
        await ViewModel.Widget.NotifyCustomizationRequestedAsync();
    }

    private void UpdateWidgetButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, updated widget");
        HideDialog();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, cancel button clicked");
        HideDialog();
    }

    private void HideDialog()
    {
        _hostingService.GetWidgetCatalog()!.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
        ViewModel.IsInEditMode = false;

        this.Hide();
    }

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        if (_widgetDefinition.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
        {
            Log.Logger()?.ReportDebug("CustomizeWidgetDialog", $"Exiting dialog, widget definition was deleted");
            _hostingService.GetWidgetCatalog()!.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;
            _dispatcher.TryEnqueue(() =>
            {
                this.Hide();
            });
        }
    }

    private void ContentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        const int ContentDialogMaxHeight = 684;

        ConfigurationContentGrid.Height = Math.Min(this.ActualHeight, ContentDialogMaxHeight) - CustomizeWidgetTitleBar.ActualHeight;

        // Subtract 80 for the margin around the button.
        ConfigurationContentViewer.Height = ConfigurationContentGrid.Height - UpdateWidgetButton.ActualHeight - 80;
    }
}
