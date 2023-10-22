// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Projects.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Projects.Views;

public partial class ProjectsPage : ToolPage, IDisposable
{
    public override string ShortName => "ProjectsPage";

    public ProjectsViewModel ViewModel { get; }

    private FileSystemWatcher _watcher;
    private bool disposedValue;

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

    private static List<RepositoryClonedEventArgs> alreadySeen = new ();

    public ProjectsPage()
    {
        ViewModel = ProjectsViewModel.CreateViewModel();
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(ProjectsViewModel.JsonFilePath), Path.GetFileName(ProjectsViewModel.JsonFilePath));
        _watcher.NotifyFilter = NotifyFilters.LastWrite;
        _watcher.Changed += (s, e) => ProjectsViewModel_ProjectsChanged(s, e);
        _watcher.EnableRaisingEvents = true;

        var eventing = Application.Current.GetService<Eventing>();
        eventing.RepositoryCloned += Eventing_RepositoryCloned;

        // cloning might have happened before we subscribed to the event
        // however as we process the events, we add them to the list of projects and serialize the viewmodel
        // so we have to keep track of which events we've already processed.
        var missedEvents = eventing.Seen.Where(x => x is RepositoryClonedEventArgs args &&
        !alreadySeen.Contains(args)).Cast<RepositoryClonedEventArgs>();

        if (missedEvents.Any())
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                var addProjectTasks = missedEvents.Select(x =>
                {
                    alreadySeen.Add(x);
                    return ViewModel.AddProject(x.RepositoryName, x.CloneLocation, x.Repository.RepoUri);
                });
                await Task.WhenAll(addProjectTasks);
                ViewModel.SerializeViewModel();
            });
        }

        InitializeComponent();
    }

    private void Eventing_RepositoryCloned(object sender, RepositoryClonedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            alreadySeen.Add(e);
            await ViewModel.AddProject(e.RepositoryName, e.CloneLocation, e.Repository.RepoUri);
        });
    }

    private void ProjectsViewModel_ProjectsChanged(object sender, System.EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.Projects.Clear();
            var newVM = ProjectsViewModel.CreateViewModel();
            foreach (var project in newVM.Projects)
            {
                ViewModel.Projects.Add(project);
            }
        });
    }

    private void AddProjectButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.SerializeViewModel();
        Process.Start(new ProcessStartInfo { FileName = ProjectsViewModel.JsonFilePath, UseShellExecute = true });
    }

    private void OnDeleteProject(object sender, ProjectViewModel project)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.Projects.Remove(project);

            // ViewModel inherits from ObservableRecipient, which has an IsActive property
            // we don't want to serialize that, so we use a custom JsonConverter
            ViewModel.SerializeViewModel();
        });
    }
}
