// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using DevHome.Projects.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Projects.Views;
public sealed partial class ProjectView : UserControl
{
    public ProjectView()
    {
        this.InitializeComponent();
    }

    public event EventHandler<ProjectViewModel> ProjectDeleted;

    private void DeleteProjectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var project = (sender as Button).DataContext as ProjectViewModel;
        ProjectDeleted?.Invoke(sender, project);
    }

    private void FilePath_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        try
        {
            var path = (sender as TextBlock).Text;
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
