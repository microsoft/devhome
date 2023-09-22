// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Windows;
public sealed partial class WindowTemplate : UserControl
{
    private readonly Window _window;

    public WindowTitleBar? TitleBar
    {
        get => this.WindowTitleBar.Content as WindowTitleBar;
        set => this.WindowTitleBar.Content = value;
    }

    public object MainContent
    {
        get => this.WindowContent.Content;
        set => this.WindowContent.Content = value;
    }

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
