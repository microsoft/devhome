// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace DevHome.ExtensionLibrary.Extensions;

public class TrueToVisibleConverter : IValueConverter
{
    public TrueToVisibleConverter()
    {
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (Microsoft.UI.Xaml.Visibility)value == Microsoft.UI.Xaml.Visibility.Visible;
    }
}
