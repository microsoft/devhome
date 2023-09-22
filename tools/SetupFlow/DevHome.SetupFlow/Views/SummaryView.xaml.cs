// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using DevHome.SetupFlow.ViewModels;
using DevHome.SetupFlow.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevHome.SetupFlow.Views;

public sealed partial class SummaryView : UserControl
{
    public SummaryView()
    {
        this.InitializeComponent();

        // Add separator dynamically between task group sections
        var sections = TaskGroupSections.Children;
        for (var i = 0; i < sections.Count - 1; ++i)
        {
            if (sections[i] is Grid sectionGrid)
            {
                sectionGrid.BorderBrush = (SolidColorBrush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
                sectionGrid.BorderThickness = new Thickness(0, 0, 0, 1);
            }
        }
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
