// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
        Thread.Sleep(300); // wait for Defender to release the lock
        for (int i = 0; i < 5; i++)
        {
            if (File.Exists(JsonFilePath))
            {
                var jsonStr = File.ReadAllText(JsonFilePath);
                var vm = JsonConvert.DeserializeObject<ProjectsViewModel>(jsonStr);
                if (vm == null)
                {
                    Thread.Sleep(300); // wait for Defender to release the lock
                    continue;
                }

                foreach (var p in vm.Projects)
                {
                    foreach (var l in p.Launchers)
                    {
                        l.ProjectViewModel = new WeakReference<ProjectViewModel>(p);
                    }
                }

                return vm;
            }
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
        Process.Start(new ProcessStartInfo { FileName = JsonFilePath, UseShellExecute = true });
    }

    private void OnDeleteProject(object sender, ProjectViewModel project)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.Projects.Remove(project);

            // ViewModel inherits from ObservableRecipient, which has an IsActive property
            // we don't want to serialize that, so we use a custom JsonConverter
            var jsonStr = JsonConvert.SerializeObject(ViewModel, Formatting.Indented, new ObservableRecipientConverter());
            File.WriteAllText(JsonFilePath, jsonStr);
        });
    }
}
