// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.PI.Models;
using DevHome.PI.ViewModels;
using DevHome.PI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DevHome.PI.Controls;

public sealed partial class ClipboardMonitorControl : UserControl
{
    private readonly ClipboardMonitorControlViewModel _viewModel = new();

    public bool ShowPopOutButton { get; set; } = true;

    public ClipboardMonitorControl()
    {
        this.InitializeComponent();
    }

    [RelayCommand]
    private void ClipboardMonitorPopOut()
    {
        ClipboardMonitoringWindow clipboardMonitoringWindow = new();
        clipboardMonitoringWindow.Activate();
    }
}
