// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.PI.Models;

namespace DevHome.PI.ViewModels;

public partial class ClipboardMonitorViewModel : ObservableObject
{
    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

    private const string EnableClipboardText = "\ue768";
    private const string DisableText = "\ue769";

    [ObservableProperty]
    private string _clipboardContentsHex = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsDec = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsCode = string.Empty;

    [ObservableProperty]
    private string _clipboardContentsHelp = string.Empty;

    [ObservableProperty]
    private string _enableClipboardButtonText = string.Empty;

    public ClipboardMonitorViewModel()
    {
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        ClipboardMonitor.Instance.PropertyChanged += Clipboard_PropertyChanged;

        if (ClipboardMonitor.Instance.IsEnabled)
        {
            EnableClipboardButtonText = DisableText;
        }
        else
        {
            EnableClipboardButtonText = EnableClipboardText;
        }
    }

    private void Clipboard_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var clipboardContents = ClipboardMonitor.Instance.Contents;
        _dispatcher.TryEnqueue(() =>
        {
            if (e.PropertyName == nameof(ClipboardMonitor.Contents))
            {
                ClipboardContentsHex = clipboardContents.Hex;
                ClipboardContentsDec = clipboardContents.Dec;
                ClipboardContentsCode = clipboardContents.Code;
                ClipboardContentsHelp = clipboardContents.Help;
            }
            else if (e.PropertyName == nameof(ClipboardMonitor.IsEnabled))
            {
                if (ClipboardMonitor.Instance.IsEnabled)
                {
                    EnableClipboardButtonText = DisableText;
                }
                else
                {
                    EnableClipboardButtonText = EnableClipboardText;
                }
            }
        });
    }
}
