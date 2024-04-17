// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Windows.FileDialog;
using DevHome.QuietBackgroundProcesses.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

namespace DevHome.QuietBackgroundProcesses.UI.Views;

public sealed partial class AnalyticSummaryPopup : ContentDialog
{
    private readonly WindowEx _mainWindow;

    public AnalyticSummaryPopupViewModel ViewModel
    {
        get;
    }

    public AnalyticSummaryPopup(QuietBackgroundProcesses.ProcessPerformanceTable? performanceTable)
    {
        _mainWindow = Application.Current.GetService<WindowEx>();

        ViewModel = new AnalyticSummaryPopupViewModel(performanceTable);

        this.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        this.DefaultButton = ContentDialogButton.Primary;
        this.InitializeComponent();
    }

    private void SaveReportButtonClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        using var fileDialog = new WindowSaveFileDialog();
        fileDialog.AddFileType("CSV files", ".csv");

        var filePath = fileDialog.Show(_mainWindow);
        if (filePath != null)
        {
            ViewModel.SaveReport(filePath);
        }
    }
}
