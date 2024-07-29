// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DevHome.PI.ViewModels;

public partial class ClipboardMonitorControlViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    [ObservableProperty]
    private string _clipboardContentsHex = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsDec = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsCode = string.Empty;

    [ObservableProperty]
    private string? _clipboardContentsHelp;

    private bool _listenForClipboardChanges = true;

    public ClipboardMonitorControlViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        ClipboardMonitor.Instance.PropertyChanged += Clipboard_PropertyChanged;

        PopulateClipboardData(ClipboardMonitor.Instance.Contents);
    }

    private void Clipboard_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var clipboardContents = ClipboardMonitor.Instance.Contents;
        _dispatcher.TryEnqueue(() =>
        {
            if (_listenForClipboardChanges)
            {
                PopulateClipboardData(clipboardContents);
            }
        });
    }

    private void PopulateClipboardData(ClipboardContents clipboardContents)
    {
        ClipboardContentsHex = clipboardContents.Hex;
        ClipboardContentsDec = clipboardContents.Dec;
        ClipboardContentsCode = clipboardContents.Code;
        ClipboardContentsHelp = string.IsNullOrEmpty(clipboardContents.Help) ? null : clipboardContents.Help;
    }

    // For these pause/resume functions, we don't want to turn off clipboard monitoring wholesale, just the UI updates
    [RelayCommand]
    public void PauseClipboardMonitoring()
    {
        _listenForClipboardChanges = false;
    }

    [RelayCommand]
    public void ResumeClipboardMonitoring()
    {
        _listenForClipboardChanges = true;
    }
}
