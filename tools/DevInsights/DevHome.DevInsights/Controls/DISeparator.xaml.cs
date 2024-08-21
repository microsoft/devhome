// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.DevInsights.Controls;

public sealed partial class DISeparator : UserControl
{
    private Orientation _orientation = Orientation.Vertical;

    public Orientation Orientation
    {
        get => _orientation;

        set
        {
            _orientation = value;

            if (value == Orientation.Vertical)
            {
                SeparatorRectangleVertical.Visibility = Visibility.Visible;
                SeparatorRectangleHorizontal.Visibility = Visibility.Collapsed;
            }
            else
            {
                SeparatorRectangleHorizontal.Visibility = Visibility.Visible;
                SeparatorRectangleVertical.Visibility = Visibility.Collapsed;
            }
        }
    }

    public DISeparator()
    {
        InitializeComponent();
    }
}
