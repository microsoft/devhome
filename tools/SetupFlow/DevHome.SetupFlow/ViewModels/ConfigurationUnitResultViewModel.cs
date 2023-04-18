// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Globalization;
using DevHome.SetupFlow.Models;
using Windows.Win32.Foundation;

namespace DevHome.SetupFlow.ViewModels;

public class ConfigurationUnitResultViewModel
{
    private readonly ConfigurationUnitResult _unitResult;

    public ConfigurationUnitResultViewModel(ConfigurationUnitResult unitResult)
    {
        _unitResult = unitResult;
    }

    public string Title => $"{_unitResult.Intent} : {_unitResult.UnitName}";

    public string Result => GetResult();

    public string GetResult()
    {
        var hresult = _unitResult.HResult.ToString("X", CultureInfo.InvariantCulture);
        if (_unitResult.IsSkipped)
        {
            return $"Skipped: {hresult}";
        }

        if (_unitResult.HResult != HRESULT.S_OK)
        {
            return $"Failed: {hresult}";
        }

        return $"Completed successfully";
    }
}
