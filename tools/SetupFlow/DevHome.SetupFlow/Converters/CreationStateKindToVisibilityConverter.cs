// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.SetupFlow.ViewModels.Environments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace DevHome.SetupFlow.Converters;

/// <summary>
/// Converts the state of the EnvironmentCreationOptions page to a visibility enum.
/// </summary>
public class CreationStateKindToVisibilityConverter : IValueConverter
{
    private const string ProgressRingGridName = "ProgressRingGrid";

    private const string AdaptiveCardGridName = "AdaptiveCardGrid";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var parameterString = (string)parameter;
        var creationStateKind = (CreationPageStateKind)value;

        if (parameterString.Equals(AdaptiveCardGridName, StringComparison.Ordinal))
        {
            return creationStateKind switch
            {
                CreationPageStateKind.InitialPageAdaptiveCardLoaded => Visibility.Visible,
                _ => Visibility.Collapsed,
            };
        }

        if (parameterString.Equals(ProgressRingGridName, StringComparison.Ordinal))
        {
            return creationStateKind switch
            {
                CreationPageStateKind.InitialPageAdaptiveCardLoading => Visibility.Visible,
                CreationPageStateKind.OtherPageAdaptiveCardLoading => Visibility.Visible,
                _ => Visibility.Collapsed,
            };
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
