// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Services;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Environments.Converters;

/// <summary>
/// Converter to convert the ComputeSystemState enum to its localized text version.
/// Note: the 'ComputeSystem' prefix should be added to every new state in the
/// resources.resw file.
/// </summary>
public class CardStateToLocalizedTextConverter : IValueConverter
{
    private static readonly StringResource _stringResource = new("DevHome.Common/Resources");
    private const string Prefix = "ComputeSystem";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var localizedText = string.Empty;

        if (value is ComputeSystemState status)
        {
            var localizationKey = Prefix + status;
            localizedText = _stringResource.GetLocalized(localizationKey);
        }

        return localizedText;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
