// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DevHome.Common.Helpers;

public class EnumToIntConverter : IValueConverter
{
    public EnumToIntConverter()
    {
    }

    /// <summary>
    /// Converts from an enum to its corresponding int value.
    /// </summary>
    /// <exception cref="ArgumentException">If the value is not an enum type</exception>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ElementTheme theme)
        {
            return (int)theme;
        }

        throw new ArgumentException("ExceptionEnumToIntConverterValueMustBeAnEnum");
    }

    /// <summary>
    /// Converts from an int to its corresponding enum value.
    /// </summary>
    /// <exception cref="ArgumentException">If the value is not an int</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is int themeNumber)
        {
            return (ElementTheme)themeNumber;
        }

        throw new ArgumentException("ExceptionEnumToIntConverterParameterMustBeAnInt");
    }
}
