// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.PI.Helpers;
using WinUIEx;

namespace DevHome.PI.Views;

public sealed partial class ClipboardMonitoringWindow : WindowEx
{
    public ClipboardMonitoringWindow()
    {
        this.InitializeComponent();
        Title = CommonHelper.GetLocalizedString("ClipboardMonitorWindowTitle");
    }
}
