// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Helpers;
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
    private WindowHelper.BinaryType binaryType = WindowHelper.BinaryType.Unknown;

    [ObservableProperty]
    private bool isPackaged = false;

    [ObservableProperty]
    private bool usesWpf = false;

    [ObservableProperty]
    private bool usesWinForms = false;

    [ObservableProperty]
    private bool usesMfc = false;

    [ObservableProperty]
    private bool isStoreApp = false;

    [ObservableProperty]
    private bool isAvalonia = false;

    [ObservableProperty]
    private bool isMaui = false;

    [ObservableProperty]
    private bool usesWinAppSdk = false;

    [ObservableProperty]
    private bool usesWinUi = false;

    [ObservableProperty]
    private bool usesDirectX = false;

    [ObservableProperty]
    private bool isRunningAsAdmin = false;

    [ObservableProperty]
    private bool isRunningAsSystem = false;

    [ObservableProperty]
    private Visibility visibility = Visibility.Visible;
}
