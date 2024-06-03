// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace DevHome.SetupFlow.Utilities;

public class DevDriveEnumToLocalizedStringConverter : IValueConverter
{
    private readonly string _prefix = "DevDrive";
    private readonly ISetupFlowStringResource _stringResource;

    public DevDriveEnumToLocalizedStringConverter()
    {
        _stringResource = Application.Current.GetService<ISetupFlowStringResource>();
    }

    // Since this is only a converter, this doesn't need to be registered as a service and can passed the string
    // resource to the constructor for testing.
    public DevDriveEnumToLocalizedStringConverter(ISetupFlowStringResource setupFlowStringResource)
    {
        _stringResource = setupFlowStringResource;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return string.Empty;
        }

        return _stringResource.GetLocalized(_prefix + value.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
