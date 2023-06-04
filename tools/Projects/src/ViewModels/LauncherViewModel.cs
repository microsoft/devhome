// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using static Windows.Win32.PInvoke;

namespace DevHome.Projects.ViewModels;

public class LauncherViewModel : ObservableObject
{
    public string DisplayName { get; set; }

    public string CommandLine { get; set; }

    public string IconPath { get; set; }

    [JsonIgnore]
    public WeakReference<ProjectViewModel> ProjectViewModel { get; set; }

    internal void Launch()
    {
        var cwd = ProjectViewModel?.TryGetTarget(out var p) == true ? p.ExpandedFilePath : null;
        var argv = GetArgv();

        var psi = new ProcessStartInfo
        {
            FileName = argv[0],
            Arguments = string.Join(" ", argv.Skip(1)),
            WorkingDirectory = cwd,
            UseShellExecute = true,
        };
        Process.Start(psi);
    }

    public string[] GetArgv()
    {
        unsafe
        {
            var args = CommandLineToArgv(CommandLine, out var argc);
            try
            {
                if (args == null)
                {
                    throw new InvalidOperationException("Failed to parse command line.");
                }

                var argv = Enumerable.Range(0, argc).Select(i => args[i].ToString()).ToArray();
                return argv;
            }
            finally
            {
                LocalFree(new Windows.Win32.Foundation.HLOCAL((IntPtr)args));
            }
        }
    }
}
