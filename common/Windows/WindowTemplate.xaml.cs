// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Windows;
public sealed partial class WindowTemplate : UserControl
{
    private readonly Window _window;

    public ContentControl WindowTitleBarControl => this.WindowTitleBar;

    public ContentControl WindowContentControl => this.WindowContent;

    public WindowTemplate(Window window)
    {
        _window = window;
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _window.ExtendsContentIntoTitleBar = true;
        _window.SetTitleBar(this.WindowTitleBar);
    }
}
