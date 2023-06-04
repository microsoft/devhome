// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevHome.Projects.ViewModels;

public class LauncherViewModel : ObservableObject
{
    public string DisplayName { get; set; }

    public string CommandLine { get; set; }

    public WeakReference<ProjectViewModel> ProjectViewModel { get; set; }

    internal void Launch()
    {
        var cwd = ProjectViewModel?.TryGetTarget(out var p) == true ? p.FilePath : null;
        var psi = new ProcessStartInfo
        {
            FileName = CommandLine,
            WorkingDirectory = cwd,
            UseShellExecute = true,
        };
        Process.Start(psi);
    }
}
