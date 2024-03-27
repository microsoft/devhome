// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.SetupFlow.Views;

public sealed partial class SummaryView : UserControl
{
    public SummaryView()
    {
        this.InitializeComponent();
    }

    public SummaryViewModel ViewModel => (SummaryViewModel)this.DataContext;

    private void ViewAllButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // When installation notes' 'view all' hyperlink is clicked, open a new window with the full text
        if (sender is HyperlinkButton hyperlinkButton && hyperlinkButton.Tag is PackageViewModel package)
        {
            var window = new InstallationNotesWindow(package.PackageTitle, package.InstallationNotes);
            window.Activate();
            window.CenterOnWindow();
        }
    }

    private void InstallationNotes_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        // Show 'view all' hyperlink if installation notes text is trimmed, otherwise hide it.
        if (sender?.Tag is HyperlinkButton viewAllButton)
        {
            viewAllButton.Visibility = sender.IsTextTrimmed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
