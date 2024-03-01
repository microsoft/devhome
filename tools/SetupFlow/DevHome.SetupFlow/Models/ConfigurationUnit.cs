// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using OutOfProcConfigurationUnit = Microsoft.Management.Configuration.ConfigurationUnit;

namespace DevHome.SetupFlow.Models;

public class ConfigurationUnit
{
    private const string DescriptionSettingsKey = "description";
    private const string ModuleMetadataKey = "module";

    public ConfigurationUnit(OutOfProcConfigurationUnit unit)
    {
        Type = unit.Type;
        Id = unit.Identifier;
        Intent = unit.Intent.ToString();
        Dependencies = [.. unit.Dependencies];

        // Get description from settings
        unit.Settings.TryGetValue(DescriptionSettingsKey, out var descriptionObj);
        Description = descriptionObj?.ToString() ?? string.Empty;

        // Get module name from metadata
        unit.Metadata.TryGetValue(ModuleMetadataKey, out var moduleObj);
        ModuleName = moduleObj?.ToString() ?? string.Empty;

        // Load dictionary values into list of key value pairs
        Settings = unit.Settings.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString())).ToList();
        Metadata = unit.Metadata.Select(m => new KeyValuePair<string, string>(m.Key, m.Value.ToString())).ToList();

        // Load details if available
        if (unit.Details != null)
        {
            // Use the module name from the details if available
            ModuleName = unit.Details.ModuleName;

            ModuleDescription = unit.Details.ModuleDescription;
            Author = unit.Details.Author;
        }
    }

    public string Type { get; }

    public string Id { get; }

    public string Description { get; }

    public string ModuleName { get; }

    public string ModuleDescription { get; }

    public string Author { get; }

    public string Intent { get; }

    public IList<string> Dependencies { get; }

    public IList<KeyValuePair<string, string>> Settings { get; }

    public IList<KeyValuePair<string, string>> Metadata { get; }
}
