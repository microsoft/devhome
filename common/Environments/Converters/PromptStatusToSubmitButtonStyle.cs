// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DevHome.Common.Environments.Converters;

/// <summary>
/// Converter to convert the PromptStatus bool value to a style that will be used by the SubmitButton.
/// </summary>
public class PromptStatusToSubmitButtonStyle : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        bool validStatus = (bool)value;

        switch (validStatus)
        {
            case true:
                return Application.Current.Resources["DefaultButtonStyle"] as Style;
            case false:
                return Application.Current.Resources["AccentButtonStyle"] as Style;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
