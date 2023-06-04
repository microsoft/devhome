// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace DevHome.Projects.ViewModels;

public class LauncherViewModel : ObservableObject
{
    public string DisplayName { get; set; }

    public string CommandLine { get; set; }

    public string IconPath { get; set; }

    [JsonIgnore]
    public WeakReference<ProjectViewModel> ProjectViewModel { get; set; }

    // pinvoke import CommandLineToArgv
    // https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-commandlinetoargvw
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

    internal void Launch()
    {
        var cwd = ProjectViewModel?.TryGetTarget(out var p) == true ? p.FilePath : null;
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
        var args = CommandLineToArgvW(CommandLine, out var argc);
        if (args == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to parse command line.");
        }

        var argv = Enumerable.Range(0, argc).Select(i => Marshal.PtrToStringUni(Marshal.ReadIntPtr(args, i * IntPtr.Size))).ToArray();
        Marshal.FreeHGlobal(args);
        return argv;
    }
}
