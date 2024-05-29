// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DevHome.Common.Converters;

public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility && (Visibility)value == Visibility.Collapsed;
    }
}
