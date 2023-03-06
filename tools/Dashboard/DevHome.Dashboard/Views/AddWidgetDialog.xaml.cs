// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace DevHome.Dashboard.Views;
public sealed partial class AddWidgetDialog : ContentDialog
{
    public AddWidgetDialog()
    {
        this.InitializeComponent();
        configurationContentFrame.Content = new WidgetConfigurationContent();
    }

    private void WidgetConfigurationNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        // Load correct adaptive card
    }

    private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        this.Hide();
    }
}
