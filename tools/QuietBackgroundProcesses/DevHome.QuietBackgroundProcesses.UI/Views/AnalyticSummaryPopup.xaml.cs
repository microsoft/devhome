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

    private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as Microsoft.UI.Xaml.Controls.TextBox;
        if (textBox != null)
        {
            var filterExpression = textBox.Text.Trim();
            ViewModel.FilterProcessesTextInputChanged(filterExpression);
        }
    }
}
