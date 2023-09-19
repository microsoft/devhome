// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Windows;
public sealed partial class WindowTemplate : UserControl
{
    private readonly Window _window;

    public object PageContent
    {
        get => GetValue(PageContentProperty);
        set => SetValue(PageContentProperty, value);
    }

    public WindowTemplate(Window window)
    {
        _window = window;
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // A custom title bar is required for full window theme and Mica support.
        // https://docs.microsoft.com/windows/apps/develop/title-bar?tabs=winui3#full-customization
        _window.ExtendsContentIntoTitleBar = true;
        _window.SetTitleBar(WindowTitleBar);
        _window.Activated += (_, e) => WindowTitleBarText.Foreground = TitleBarHelper.GetTitleBarTextColorBrush(e.WindowActivationState);
    }

    public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(nameof(PageContent), typeof(object), typeof(WindowTemplate), new PropertyMetadata(null));
}
