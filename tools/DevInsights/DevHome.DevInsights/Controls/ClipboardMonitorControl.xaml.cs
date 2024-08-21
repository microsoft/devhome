// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.DevInsights.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.Controls;

public sealed partial class ClipboardMonitorControl : UserControl
{
    private readonly ClipboardMonitorControlViewModel _viewModel = new();

    public ClipboardMonitorControl()
    {
        this.InitializeComponent();
    }
}
