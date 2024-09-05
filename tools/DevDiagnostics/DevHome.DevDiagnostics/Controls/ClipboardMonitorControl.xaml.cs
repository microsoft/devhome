// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.DevDiagnostics.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevDiagnostics.Controls;

public sealed partial class ClipboardMonitorControl : UserControl
{
    private readonly ClipboardMonitorControlViewModel _viewModel = new();

    public ClipboardMonitorControl()
    {
        this.InitializeComponent();
    }
}
