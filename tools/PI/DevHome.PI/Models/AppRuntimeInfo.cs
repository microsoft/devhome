// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace DevHome.PI.Models;

public partial class AppRuntimeInfo : ObservableObject
{
    [ObservableProperty]
    private int processId = 0;

    [ObservableProperty]
    private int basePriority = 0;

    [ObservableProperty]
    private int priorityClass = 0;

    [ObservableProperty]
    private string mainModuleFileName = string.Empty;

    [ObservableProperty]
    private string cpuArchitecture = string.Empty;

    [ObservableProperty]
    private bool isPackaged = false;

    [ObservableProperty]
    private bool isStoreApp = false;

    [ObservableProperty]
    private bool isRunningAsAdmin = false;

    [ObservableProperty]
    private bool isRunningAsSystem = false;

    [ObservableProperty]
    private Visibility visibility = Visibility.Visible;

    public ObservableCollection<FrameworkType> FrameworkTypes { get; } = [];

    public AppRuntimeInfo()
    {
        // Note these are in alphabetical order of Name.
        FrameworkTypes.Add(new FrameworkType("Avalonia.Base.dll", "Avalonia"));
        FrameworkTypes.Add(new FrameworkType("DXCore.dll", "DirectX"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.Maui.dll", "Maui"));
        FrameworkTypes.Add(new FrameworkType("MFC", "MFC", false));
        FrameworkTypes.Add(new FrameworkType("Python.exe", "Python"));
        FrameworkTypes.Add(new FrameworkType("OneCoreUAPCommonProxyStub.dll", "UWP"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.Windows.SDK.NET.dll", "Windows App SDK"));
        FrameworkTypes.Add(new FrameworkType("System.Windows.Forms.dll", "Windows Forms"));
        FrameworkTypes.Add(new FrameworkType("Microsoft.WinUI.dll", "WinUI"));
        FrameworkTypes.Add(new FrameworkType("PresentationFramework.dll", "WPF"));
    }

    public void CheckFrameworkTypes(string moduleName)
    {
        foreach (var item in FrameworkTypes)
        {
            // Skip if already matched.
            if (item.IsTypeSupported == true)
            {
                continue;
            }

            if (item.IsExactMatch)
            {
                if (moduleName.Equals(item.Determinator, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsTypeSupported = true;
                }
            }
            else
            {
                if (moduleName.Contains(item.Determinator, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsTypeSupported = true;
                }
            }
        }
    }

    public void CheckFrameworkTypes(ProcessModuleCollection modules)
    {
        foreach (ProcessModule module in modules)
        {
            CheckFrameworkTypes(module.ModuleName);
        }

        // Special-case for UWP apps:
        // UWP will be set true if we found OneCoreUAPCommonProxyStub.dll, but this is also used
        // in Maui and WinUI apps. So if we also found Maui or WinUI, we set UWP to false.
        var uwpItem = FrameworkTypes.First(item => item.Name.Equals("UWP", StringComparison.OrdinalIgnoreCase));
        if (uwpItem.IsTypeSupported)
        {
            var mauiItem = FrameworkTypes.First(item => item.Name.Equals("Maui", StringComparison.OrdinalIgnoreCase));
            var winUIItem = FrameworkTypes.First(item => item.Name.Equals("WinUI", StringComparison.OrdinalIgnoreCase));
            if (mauiItem.IsTypeSupported || winUIItem.IsTypeSupported)
            {
                uwpItem.IsTypeSupported = false;
            }
        }
    }
}
