// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.ViewModels;

public class ConfigurationUnitViewModel
{
    private readonly ConfigurationUnit _configurationUnit;

    public ConfigurationUnitViewModel(ConfigurationUnit configurationUnit)
    {
        _configurationUnit = configurationUnit;
    }

    public string Type => _configurationUnit.Type;

    public string Description => _configurationUnit.Description;

    public string ModuleName => _configurationUnit.ModuleName;

    public string Intent => _configurationUnit.Intent;

    public string Settings => string.Join('\n', _configurationUnit.Settings);
}
