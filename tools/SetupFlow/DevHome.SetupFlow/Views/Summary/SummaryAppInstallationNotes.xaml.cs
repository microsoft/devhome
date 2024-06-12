// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views.Summary;

public sealed partial class SummaryAppInstallationNotes : UserControl
{
    public SummaryAppInstallationNotes()
    {
        this.InitializeComponent();
    }

    private void ViewAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // When installation notes' 'view all' button is clicked, open a new window with the full text
        if (sender is Button viewAllButton && viewAllButton.Tag is PackageViewModel package)
        {
            var window = new InstallationNotesWindow(package.PackageTitle, package.InstallationNotes);
            window.CenterOnWindow();
            window.Activate();
        }
    }

    private void InstallationNotes_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        // Show 'view all' button if installation notes text is trimmed, otherwise hide it.
        if (sender?.Tag is Button viewAllButton)
        {
            viewAllButton.Visibility = sender.IsTextTrimmed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
