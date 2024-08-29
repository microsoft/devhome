// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.DevInsights.Helpers;
using WinUIEx;

namespace DevHome.DevInsights.Views;

public sealed partial class ClipboardMonitoringWindow : WindowEx
{
    public ClipboardMonitoringWindow()
    {
        this.InitializeComponent();
        Title = CommonHelper.GetLocalizedString("ClipboardMonitorWindowTitle");
    }
}
