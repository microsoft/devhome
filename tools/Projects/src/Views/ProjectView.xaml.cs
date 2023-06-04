// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Projects.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
