// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.PI.Models;
using Microsoft.UI.Xaml;

namespace DevHome.PI;

public partial class BarWindow
{
    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // If we receive a window closed event, clean up the system
        TargetAppData.Instance.ClearAppData();

        var primaryWindow = Application.Current.GetService<PrimaryWindow>();
        primaryWindow.ClearBarWindow();
    }
}
