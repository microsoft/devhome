// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Windows;

/// <summary>
/// A title bar that can be used in place of the default title bar when the
/// host window set <see cref="Window.ExtendsContentIntoTitleBar"/> to true.
/// </summary>
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

    public void Repaint()
    {
        // Update the title text block foreground from code behind after the
        // window activation state or system theme has changed, and after the WindowCaption*
        // brushes have been updated. More details in TitleBarHelper.UpdateTitleBar method.
        TitleTextBlock.Foreground = TitleBarHelper.GetTitleBarTextColorBrush(IsActive);
    }

    private static void OnTitleChanged(WindowTitleBar windowTitleBar, string newValue)
    {
        windowTitleBar.TitleChanged?.Invoke(windowTitleBar, newValue);
    }

    private static void OnIsActiveChanged(WindowTitleBar windowTitleBar, bool newValue)
    {
        windowTitleBar.Repaint();
    }

    private static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(WindowTitleBar), new PropertyMetadata(null, (s, e) => OnTitleChanged((WindowTitleBar)s, (string)e.NewValue)));
    private static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(WindowTitleBar), new PropertyMetadata(null));
    private static readonly DependencyProperty HideIconProperty = DependencyProperty.Register(nameof(HideIcon), typeof(bool), typeof(WindowTitleBar), new PropertyMetadata(false));
    private static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(WindowTitleBar), new PropertyMetadata(true, (s, e) => OnIsActiveChanged((WindowTitleBar)s, (bool)e.NewValue)));
}
