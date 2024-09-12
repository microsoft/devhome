// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.DevDiagnostics.Controls;
using Microsoft.UI.Xaml;

namespace DevHome.DevDiagnostics.Views;

public sealed partial class ClipboardMonitoringWindow : ThemeAwareWindow
{
    public ClipboardMonitoringWindow()
    {
        InitializeComponent();
    }

    private void ThemeAwareWindow_Closed(object sender, WindowEventArgs args)
    {
        var barWindow = Application.Current.GetService<PrimaryWindow>().DBarWindow;
        barWindow?.RemoveRelatedWindow(this);
    }
}
