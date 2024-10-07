// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Common.Converters;

/// <summary>
/// Filters out strings from a list of strings that can be shown in the UI.
/// </summary>
public class ExcludeStringsFromListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is List<string> originalList && parameter is string filterParameter)
        {
            var filters = filterParameter
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim());

            return new List<string>(originalList.Where(item => !filters.Contains(item)));
        }

        return new();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
