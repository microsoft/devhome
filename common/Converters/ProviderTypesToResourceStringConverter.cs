// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Data;

namespace DevHome.Common.Converters;

/// <summary>
/// Converts a list of provider type strings to localized strings that can be found in
/// DevHome.Common.pri. The CommandParameter is used to filter out specific provider type
/// strings from the list and is expected to be a comma separated string where each value
/// is a provider type.
/// </summary>
public class ProviderTypesToResourceStringConverter : IValueConverter
{
    private readonly StringResource _stringResourceCommon = new("DevHome.Common.pri", "DevHome.Common/Resources");

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var localizedStrings = new List<string>();
        if (value is not List<string> providerList)
        {
            return localizedStrings;
        }

        IEnumerable<string> parameterFilter = new List<string>();

        if (parameter is string parametersList)
        {
            parameterFilter = parametersList
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()) ?? parameterFilter;
        }

        foreach (var item in providerList)
        {
            if (!parameterFilter.Contains(item))
            {
                localizedStrings.Add(_stringResourceCommon.GetLocalized($"{item}ProviderType"));
            }
        }

        return localizedStrings;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
