// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.DevDiagnostics.Helpers;
using WinUIEx;

namespace DevHome.DevDiagnostics.Views;

public sealed partial class ClipboardMonitoringWindow : WindowEx
{
    public ClipboardMonitoringWindow()
    {
        this.InitializeComponent();
        Title = CommonHelper.GetLocalizedString("ClipboardMonitorWindowTitle");
    }
}
