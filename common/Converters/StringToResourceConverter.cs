// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Data;

namespace DevHome.Common.Converters;

/// <summary>
/// Converts a resource key name located in a project to its localized string.
/// The CommandParameter should be the name of the project the .pri file is located in.
/// </summary>
public class StringToResourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string resourceKey || parameter is not string project)
        {
            return string.Empty;
        }

        StringResource resource = new($"{project}.pri", $"{project}/Resources");

        return resource.GetLocalized(resourceKey);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
