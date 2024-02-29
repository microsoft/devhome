// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Contracts.Services;
using DevHome.Dashboard.Helpers;
using DevHome.Dashboard.Services;
using DevHome.Dashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Widgets.Hosts;
using WinUIEx;

namespace DevHome.Dashboard.Views;

public sealed partial class AddWidgetDialog : ContentDialog
{
    private WidgetDefinition _selectedWidget;

    public WidgetDefinition AddedWidget { get; private set; }

    public AddWidgetViewModel ViewModel { get; set; }

    private readonly WindowEx _windowEx;

    public AddWidgetDialog()
    {
        ViewModel = Application.Current.GetService<AddWidgetViewModel>();
        _hostingService = Application.Current.GetService<IWidgetHostingService>();
        _widgetIconService = Application.Current.GetService<IWidgetIconService>();

        this.InitializeComponent();

        _windowEx = Application.Current.GetService<WindowEx>();

        RequestedTheme = Application.Current.GetService<IThemeSelectorService>().Theme;
    }

    [RelayCommand]
    public async Task OnLoadedAsync()
    {
        var widgetCatalog = await _hostingService.GetWidgetCatalogAsync();
        widgetCatalog.WidgetDefinitionDeleted += WidgetCatalog_WidgetDefinitionDeleted;

        await FillAvailableWidgetsAsync();
        SelectFirstWidgetByDefault();
    }

    private void SelectFirstWidgetByDefault()
    {
        if (AddWidgetNavigationView.MenuItems.Count > 0)
        {
            var firstProvider = AddWidgetNavigationView.MenuItems[0] as NavigationViewItem;
            if (firstProvider.MenuItems.Count > 0)
            {
                var firstWidget = firstProvider.MenuItems[0] as NavigationViewItem;
                AddWidgetNavigationView.SelectedItem = firstWidget;
            }
        }
    }

    private async void AddWidgetNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        // Selected item could be null if list of widgets became empty, but list should never be empty
        // since core widgets are always available.
        if (sender.SelectedItem is null)
        {
            ViewModel.Clear();
            return;
        }

        // Get selected widget definition.
        var selectedTag = (sender.SelectedItem as MenuItemViewModel).Tag;
        if (selectedTag is null)
        {
            Log.Logger()?.ReportError("AddWidgetDialog", $"Selected widget description did not have a tag");
            ViewModel.Clear();
            return;
        }

        // If the user has selected a widget, show preview. If they selected a provider, leave space blank.
        if (selectedTag as WidgetDefinition is WidgetDefinition selectedWidgetDefinition)
        {
            _selectedWidget = selectedWidgetDefinition;
            await ViewModel.SetWidgetDefinition(selectedWidgetDefinition);
        }
        else if (selectedTag as WidgetProviderDefinition is not null)
        {
            ViewModel.Clear();
        }
    }

    [RelayCommand]
    private async Task UpdateThemeAsync()
    {
        // Update the icons for each available widget listed.
        foreach (var providerItem in AddWidgetNavigationView.MenuItems.Cast<NavigationViewItem>())
        {
            foreach (var widgetItem in providerItem.MenuItems.Cast<NavigationViewItem>())
            {
                var widgetDefinition = widgetItem.Tag as WidgetDefinition;
                var image = await _widgetIconService.GetWidgetIconForThemeAsync(widgetDefinition, ActualTheme);
                widgetItem.Content = BuildNavItem(image, widgetDefinition.DisplayTitle);
            }
        }
    }

    [RelayCommand]
    private void PinButtonClick()
    {
        Log.Logger()?.ReportDebug("AddWidgetDialog", $"Pin selected");
        AddedWidget = _selectedWidget;

        HideDialogAsync();
    }

    [RelayCommand]
    private void CancelButtonClick()
    {
        Log.Logger()?.ReportDebug("AddWidgetDialog", $"Canceled dialog");
        AddedWidget = null;

        HideDialogAsync();
    }

    private async void HideDialogAsync()
    {
        _selectedWidget = null;
        ViewModel = null;

        var widgetCatalog = await _hostingService.GetWidgetCatalogAsync();
        widgetCatalog!.WidgetDefinitionDeleted -= WidgetCatalog_WidgetDefinitionDeleted;

        this.Hide();
    }

    private void WidgetCatalog_WidgetDefinitionDeleted(WidgetCatalog sender, WidgetDefinitionDeletedEventArgs args)
    {
        var deletedDefinitionId = args.DefinitionId;

        _windowEx.DispatcherQueue.TryEnqueue(() =>
        {
            // If we currently have the deleted widget open, un-select it.
            if (_selectedWidget is not null &&
                _selectedWidget.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
            {
                Log.Logger()?.ReportInfo("AddWidgetDialog", $"Widget definition deleted while selected.");
                ViewModel.Clear();
                AddWidgetNavigationView.SelectedItem = null;
            }

            // Remove the deleted WidgetDefinition from the list of available widgets.
            foreach (var providerItem in ProviderMenuItems)
            {
                foreach (var widgetItem in providerItem.SubMenuItems)
                {
                    if (widgetItem.Tag is WidgetDefinition tagDefinition)
                    {
                        if (tagDefinition.Id.Equals(deletedDefinitionId, StringComparison.Ordinal))
                        {
                            providerItem.SubMenuItems.Remove(widgetItem);

                            // If we've removed all widgets from a provider, remove the provider from the list.
                            if (!providerItem.SubMenuItems.Any())
                            {
                                ProviderMenuItems.Remove(providerItem);

                                // If we've removed all providers from the list, log an error.
                                // This should never happen since Dev Home's core widgets are always available.
                                if (!ProviderMenuItems.Any())
                                {
                                    Log.Logger()?.ReportError("AddWidgetDialog", $"WidgetCatalog_WidgetDefinitionDeleted found no available widgets.");
                                }
                            }

                            return;
                        }
                    }
                }
            }
        });
    }

    private void ContentDialog_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var contentDialogMaxHeight = (double)Resources["ContentDialogMaxHeight"];
        const int SmallThreshold = 324;
        const int MediumThreshold = 360;

        var smallPinButtonMargin = (Thickness)Resources["SmallPinButtonMargin"];
        var largePinButtonMargin = (Thickness)Resources["LargePinButtonMargin"];
        var smallWidgetPreviewTopMargin = (Thickness)Resources["SmallWidgetPreviewTopMargin"];
        var largeWidgetPreviewTopMargin = (Thickness)Resources["LargeWidgetPreviewTopMargin"];

        AddWidgetNavigationView.Height = Math.Min(this.ActualHeight, contentDialogMaxHeight) - AddWidgetTitleBar.ActualHeight;

        var previewHeightAvailable = AddWidgetNavigationView.Height - TitleRow.ActualHeight - PinRow.ActualHeight;

        // Adjust margins when the height gets too small to show everything.
        if (previewHeightAvailable < SmallThreshold)
        {
            PreviewRow.Padding = smallWidgetPreviewTopMargin;
            PinButton.Margin = smallPinButtonMargin;
        }
        else if (previewHeightAvailable < MediumThreshold)
        {
            PreviewRow.Padding = smallWidgetPreviewTopMargin;
            PinButton.Margin = largePinButtonMargin;
        }
        else
        {
            PreviewRow.Padding = largeWidgetPreviewTopMargin;
            PinButton.Margin = largePinButtonMargin;
        }
    }
}
