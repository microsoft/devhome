// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace DevHome.SetupFlow.Models;

public class ConfigurationUnit
{
    private const string DescriptionSettingsKey = "description";
    private const string ModuleMetadataKey = "module";

    public ConfigurationUnit(Microsoft.Management.Configuration.ConfigurationUnit unit)
    {
        Type = unit.Type;
        Id = unit.Identifier;

        // Get description from settings
        unit.Settings.TryGetValue(DescriptionSettingsKey, out var descriptionObj);
        Description = descriptionObj?.ToString() ?? string.Empty;

        // Get module name from metadata
        unit.Metadata.TryGetValue(ModuleMetadataKey, out var moduleObj);
        ModuleName = moduleObj?.ToString() ?? string.Empty;

        Intent = unit.Intent.ToString();
        Settings = unit.Settings.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString())).ToList();
        Metadata = unit.Metadata.Select(m => new KeyValuePair<string, string>(m.Key, m.Value.ToString())).ToList();
    }

    public string Type { get; set; }

    public string Id { get; set; }

    public string Description { get; set; }

    public string ModuleName { get; set; }

    public string Intent { get; set; }

    public IList<KeyValuePair<string, string>> Settings { get; set; }

    public IList<KeyValuePair<string, string>> Metadata { get; set; }
}
