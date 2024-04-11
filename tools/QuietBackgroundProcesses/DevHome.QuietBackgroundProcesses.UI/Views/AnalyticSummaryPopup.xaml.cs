// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.QuietBackgroundProcesses.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.QuietBackgroundProcesses.UI.Views;

public sealed partial class AnalyticSummaryPopup : ContentDialog
{
    public AnalyticSummaryPopupViewModel ViewModel
    {
        get;
    }

    public AnalyticSummaryPopup(QuietBackgroundProcesses.ProcessPerformanceTable? performanceTable)
    {
        ViewModel = new AnalyticSummaryPopupViewModel(performanceTable);

        this.InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }
}
