// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Windows;

public sealed partial class WindowTitleBar : UserControl
{
    public event EventHandler<string>? TitleChanged;

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IconElement Icon
    {
        get => (IconElement)GetValue(IconProperty) ?? DefaultIconContent;
        set => SetValue(IconProperty, value);
    }

    public bool HideIcon
    {
        get => (bool)GetValue(HideIconProperty);
        set => SetValue(HideIconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the title bar is active.
    /// </summary>
    /// <remarks>
    /// <para>Title bars help users differentiate when a window is active and
    /// inactive. All title bar elements should be semi-transparent when the
    /// window is inactive.</para>
    /// <para>Reference: <a href="https://learn.microsoft.com/en-us/windows/apps/design/basics/titlebar-design#bar" /></para>
    /// </remarks>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public WindowTitleBar()
    {
        this.InitializeComponent();
    }

    private static void OnTitleChanged(WindowTitleBar windowTitleBar, string newValue)
    {
        windowTitleBar.TitleChanged?.Invoke(windowTitleBar, newValue);
    }

    private static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(WindowTitleBar), new PropertyMetadata(null, (s, e) => OnTitleChanged((WindowTitleBar)s, (string)e.NewValue)));
    private static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(WindowTitleBar), new PropertyMetadata(null));
    private static readonly DependencyProperty HideIconProperty = DependencyProperty.Register(nameof(HideIcon), typeof(bool), typeof(WindowTitleBar), new PropertyMetadata(false));
    private static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(WindowTitleBar), new PropertyMetadata(true));
}
