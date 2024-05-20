// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Common.Views;

/// <summary>
/// A button whose content is a + icon and a string. Used for creating new items.
/// </summary>
public sealed partial class AddCreateButton : Button
{
    /// <summary>
    /// Gets or Sets the Content to display on the Button alongside the icon.
    /// </summary>
    public new string Content
    {
        get => (string)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public AddCreateButton()
    {
        this.InitializeComponent();
    }

    public static readonly new DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(string), typeof(Button), new PropertyMetadata(string.Empty));
}
