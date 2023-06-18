// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;

namespace DevHome.Projects.ViewModels;
public partial class ProjectViewModel : ObservableObject, IDisposable
{
    public string Name { get; set; }

    public string Url { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpandedFilePath))]
    private string filePath;

    [JsonIgnore]
    public string ExpandedFilePath => Environment.ExpandEnvironmentVariables(FilePath);

    public string Color { get; set; } = "Transparent";

    public ObservableCollection<LauncherViewModel> Launchers { get; } = new ();

    [JsonIgnore]
    [ObservableProperty]
    private string branch;

    [JsonIgnore]
    private DispatcherQueue _dispatcherQueue;

    public ProjectViewModel()
    {
        this.PropertyChanged += (a, b) =>
        {
            if (b.PropertyName == nameof(FilePath))
            {
                ResetWatcher();
            }
        };
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    private FileSystemWatcher _watcher;

    private bool disposedValue;

    private void ResetWatcher()
    {
        if (_watcher != null)
        {
            _watcher.Dispose();
            _watcher = null;
        }

        if (string.IsNullOrEmpty(FilePath))
        {
            return;
        }

        var dir = Path.Combine(ExpandedFilePath/*, ".git"*/);
        var file = "HEAD";
        if (!Directory.Exists(ExpandedFilePath))
        {
            return;
        }

        _watcher = new FileSystemWatcher(dir, file)
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
        };
        _watcher.Changed += (_, b) =>
        {
            UpdateBranch();
        };
        UpdateBranch();
    }

    private void UpdateBranch()
    {
        Thread.Sleep(300); // wait for Defender to release the lock
        var newBranch = GetBranch();
        if (_dispatcherQueue.HasThreadAccess)
        {
            Debug.WriteLine($"On thread. newBranch={newBranch}");
            Branch = newBranch;
        }
        else
        {
            Debug.WriteLine($"Not on thread. newBranch={newBranch}");
            _dispatcherQueue.TryEnqueue(() => Branch = newBranch);
        }
    }

    private string GetBranch()
    {
        try
        {
            var dir = Path.Combine(ExpandedFilePath, ".git");
            var file = "HEAD";
            var head = File.ReadAllText(Path.Combine(dir, file));
            var newBranch = string.Join('/', head.Split('/').Skip(2)); // skip refs/heads
            return newBranch.Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _watcher.Dispose();
                _watcher = null;
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
}
