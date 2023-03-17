// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.IO;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.DevDrive.ViewModels;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace DevHome.SetupFlow.DevDrive.Windows;
public sealed partial class DevDriveWindow : WindowEx
{
    // TODO: This icon is subject to change, when Dev Home gets a new icon along with a more global way to
    // access it since its not set programmatically currently, only through xaml.
    private readonly string _devHomeIconPath = "Assets/WindowIcon.ico";
    private readonly DevDriveViewModel _devDriveViewModel;
    private readonly WindowEx _mainWindow;

    public DevDriveWindow(DevDriveViewModel viewModel)
    {
        this.InitializeComponent();
        this.SetIcon(Path.Combine(AppContext.BaseDirectory, _devHomeIconPath));
        _devDriveViewModel = viewModel;
        _mainWindow = Application.Current.GetService<WindowEx>();
        Title = _mainWindow.Title;
    }

    public DevDriveViewModel ViewModel => _devDriveViewModel;
}
