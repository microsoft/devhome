// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Environments.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace DevHome.Common.Environments.Converters;

/// <summary>
/// Converter to convert the CardStateColor enum value to a brush that will be displayed in the Environment Card.
/// </summary>
public class CardStateColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        SolidColorBrush signalBrush = new();

        if (value is CardStateColor status)
        {
            signalBrush = status switch
            {
                CardStateColor.Success => (SolidColorBrush)Application.Current.Resources["SystemFillColorSuccessBrush"],
                CardStateColor.Neutral => (SolidColorBrush)Application.Current.Resources["SystemFillColorSolidNeutralBrush"],
                CardStateColor.Caution => (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"],
                CardStateColor.Failure => (SolidColorBrush)Application.Current.Resources["SystemFillColorCriticalBrush"],
                _ => (SolidColorBrush)Application.Current.Resources["SystemFillColorCautionBrush"],
            };
        }

        return signalBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
