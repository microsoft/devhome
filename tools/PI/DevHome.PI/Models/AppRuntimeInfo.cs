// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.PI.Models;

public partial class AppRuntimeInfo : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AppRuntimeInfo));

    private readonly string _unknownText = CommonHelper.GetLocalizedString("UnknownText");

    private const string UwpCommandLinePattern = @"^(.*) -ServerName:(.*?)\.AppX(.*?)\.mca$";

    [GeneratedRegex(UwpCommandLinePattern)]
    private static partial Regex UwpCommandLineRegex();

    [ObservableProperty]
    private int _processId = 0;

    [ObservableProperty]
    private int _basePriority = 0;

    [ObservableProperty]
    private int _priorityClass = 0;

    [ObservableProperty]
    private string _mainModuleFileName = string.Empty;

    [ObservableProperty]
    private string _cpuArchitecture = string.Empty;

    [ObservableProperty]
    private bool _isPackaged = false;

    [ObservableProperty]
    private bool _isRunningAsAdmin = false;

    [ObservableProperty]
    private bool _isRunningAsSystem = false;

    [ObservableProperty]
    private Visibility _visibility = Visibility.Visible;

    [ObservableProperty]
    private string _activationArgs = string.Empty;

    [ObservableProperty]
    private PackageInfo _packageInfo = new();

    [ObservableProperty]
    private string _identifiedFrameWorkTypes = string.Empty;

    private List<FrameworkType> FrameworkTypes { get; } = [];

    public AppRuntimeInfo()
    {
        FrameworkTypes.Add(new FrameworkType("Avalonia.Base.dll", "Avalonia"));
        FrameworkTypes.Add(new FrameworkType("DXCore.dll", "DirectX"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.Maui.dll", "Maui"));
        FrameworkTypes.Add(new FrameworkType("MFC", "MFC", false));
        FrameworkTypes.Add(new FrameworkType("Python.exe", "Python"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.Web.WebView2.Core.dll", "WebView2"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.Windows.SDK.NET.dll", "Windows App SDK"));
        FrameworkTypes.Add(new FrameworkType("System.Windows.Forms.dll", "Windows Forms"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.UI.Xaml.dll", "WinUI 2"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.UI.Xaml.Controls.dll", "WinUI 3"));
        FrameworkTypes.Add(new FrameworkType("PresentationFramework.dll", "WPF"));
    }

    public void GetFrameworksAndCommandLine(Process process)
    {
        var identifiedFrameworks = new List<string>();
        var modules = process.Modules;
        foreach (ProcessModule module in modules)
        {
            foreach (var item in FrameworkTypes)
            {
                if (identifiedFrameworks.Contains(item.Name))
                {
                    continue;
                }

                if (item.IsExactMatch)
                {
                    if (module.ModuleName.Equals(item.Determinator, StringComparison.OrdinalIgnoreCase))
                    {
                        identifiedFrameworks.Add(item.Name);
                    }
                }
                else
                {
                    if (module.ModuleName.Contains(item.Determinator, StringComparison.OrdinalIgnoreCase))
                    {
                        identifiedFrameworks.Add(item.Name);
                    }
                }
            }
        }

        // Both WinUI2 and WinUI3 use Microsoft.UI.Xaml.dll, but only WinUI3 uses Microsoft.UI.Xaml.Controls.dll.
        if (identifiedFrameworks.Contains("WinUI 3"))
        {
            identifiedFrameworks.Remove("WinUI 2");
        }

        IdentifiedFrameWorkTypes = string.Join(", ", identifiedFrameworks);

        /* The only reliable check for UWP is if the command-line matches the known UWP pattern.
        Examples:
        "C:\Program Files\WindowsApps\Microsoft.WindowsAlarms_11.2406.47.0_x64__8wekyb3d8bbwe\Time.exe" -ServerName:App.AppXq8avk61zazpy808ab5ppkf6taqp47km6.mca
        "C:\Program Files\WindowsApps\35455AndrewWhitechapel.uTaskManager_2309.21.1.0_x64__6rjrek5qak82t\uTaskManager.exe" -ServerName:uTaskManager.AppXjaq7n2ahxkbe1kpkhkxqhr5d0s2yr0pb.mca
        "C:\Foo\TestApps\UwpAea\UwpAea\bin\x86\Debug\AppX\UwpAea.exe" -ServerName:Blueberry.Pie.AppXnzm9t7zr5rgagha6146e9rgzyahj42xx.mca
        */
        GetCommandLine(process);
        if (UwpCommandLineRegex().IsMatch(ActivationArgs))
        {
            IdentifiedFrameWorkTypes += ", UWP";
            ActivationArgs = _unknownText;
        }
    }

    private void GetCommandLine(Process process)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
            if (searcher is null)
            {
                return;
            }

            using var objects = searcher.Get();
            if (objects is null)
            {
                return;
            }

            var obj = objects.Cast<ManagementObject>().FirstOrDefault();
            if (obj is not null)
            {
                ActivationArgs = obj["CommandLine"]?.ToString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to get command line for process {ProcessId}", process.Id);
        }
    }
}
