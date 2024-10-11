﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

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

        var a = resource.GetLocalized(resourceKey);
        return a;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
