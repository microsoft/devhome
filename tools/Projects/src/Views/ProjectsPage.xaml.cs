// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using DevHome.Common;
using DevHome.Projects.ViewModels;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace DevHome.Projects.Views;

public partial class ProjectsPage : ToolPage, IDisposable
{
    public override string ShortName => "ProjectsPage";

    public ProjectsViewModel ViewModel { get; }

    private FileSystemWatcher _watcher;
    private bool disposedValue;

    private static string JsonFilePath => Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\DevHome.projects.json");

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _watcher.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public static ProjectsViewModel CreateViewModel()
    {
        if (File.Exists(JsonFilePath))
        {
            var jsonStr = File.ReadAllText(JsonFilePath);
            var vm = JsonConvert.DeserializeObject<ProjectsViewModel>(jsonStr);
            foreach (var p in vm.Projects)
            {
                p.FilePath = Environment.ExpandEnvironmentVariables(p.FilePath);
                foreach (var l in p.Launchers)
                {
                    l.ProjectViewModel = new WeakReference<ProjectViewModel>(p);
                }
            }

            return vm;
        }

        return new ProjectsViewModel();
    }

    public ProjectsPage()
    {
        ViewModel = CreateViewModel();
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(JsonFilePath), Path.GetFileName(JsonFilePath));
        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        _watcher.Changed += (s, e) => ProjectsViewModel_ProjectsChanged(s, e);
        _watcher.EnableRaisingEvents = true;
        InitializeComponent();
    }

    private void ProjectsViewModel_ProjectsChanged(object sender, System.EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.Projects.Clear();
            var newVM = CreateViewModel();
            foreach (var project in newVM.Projects)
            {
                ViewModel.Projects.Add(project);
            }
        });
    }

    private void AddProjectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
    }

    private void DeleteProjectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
    }

    private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        Process.Start(new ProcessStartInfo(sender.NavigateUri.AbsoluteUri));
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

    private void LauncherButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var project = (sender as Button).DataContext as LauncherViewModel;
        project?.Launch();
    }
}
