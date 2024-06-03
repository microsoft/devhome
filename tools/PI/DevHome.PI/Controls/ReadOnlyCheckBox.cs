// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevHome.PI.Controls;

public class ReadOnlyCheckBox : CheckBox
{
    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(ReadOnlyCheckBox),
            new PropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    protected override void OnToggle()
    {
        if (!IsReadOnly)
        {
            base.OnToggle();
        }
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        if (!IsReadOnly)
        {
            base.OnPointerPressed(e);
        }
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!IsReadOnly)
        {
            base.OnKeyDown(e);
        }
    }
}
