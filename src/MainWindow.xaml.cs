// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Helpers;
using Microsoft.UI.Xaml;

namespace DevHome;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/DevHome.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
    }

    private void MainWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        Application.Current.GetService<IPluginService>().SignalStopPluginsAsync();
    }
}
