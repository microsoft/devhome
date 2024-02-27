// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Models;

public class ConfigurationUnit
{
    public ConfigurationUnit(Microsoft.Management.Configuration.ConfigurationUnit unit)
    {
        Type = unit.Type;
        Id = unit.Identifier;
        unit.Settings.TryGetValue("description", out var descriptionObj);
        Description = descriptionObj?.ToString() ?? string.Empty;
        Intent = unit.Intent.ToString();
        Settings = [];
        foreach (var setting in unit.Settings)
        {
            Settings.Add($"{setting.Key} : {setting.Value}");
        }
    }

    public string Type { get; set; }

    public string Id { get; set; }

    public string Description { get; set; }

    public string ModuleName { get; set; }

    public string Intent { get; set; }

    public IList<string> Settings { get; set; }
}
