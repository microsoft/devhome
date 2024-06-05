// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.Models;

public class DSCConfigurationUnit
{
    private const string DescriptionMetadataKey = "description";
    private const string ModuleMetadataKey = "module";

    public DSCConfigurationUnit(ConfigurationUnit unit)
    {
        // Constructor copies all the required data from the out-of-proc COM
        // objects over to the current process. This ensures that we have this
        // information available even if the out-of-proc COM objects are no
        // longer available (e.g. AppInstaller service is no longer running).
        Type = unit.Type;
        Id = unit.Identifier;
        Intent = unit.Intent.ToString();
        Dependencies = [.. unit.Dependencies];

        // Get description from settings
        unit.Metadata.TryGetValue(DescriptionMetadataKey, out var descriptionObj);
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
            UnitType = unit.Details.UnitType;
            UnitDescription = unit.Details.UnitDescription;
            UnitDocumentationUri = unit.Details.UnitDocumentationUri?.ToString();
            ModuleName = unit.Details.ModuleName;
            ModuleType = unit.Details.ModuleType;
            ModuleSource = unit.Details.ModuleSource;
            ModuleDescription = unit.Details.ModuleDescription;
            ModuleDocumentationUri = unit.Details.ModuleDocumentationUri?.ToString();
            PublishedModuleUri = unit.Details.PublishedModuleUri?.ToString();
            Version = unit.Details.Version;
            IsLocal = unit.Details.IsLocal;
            Author = unit.Details.Author;
            Publisher = unit.Details.Publisher;
            IsPublic = unit.Details.IsPublic;
        }
    }

    public string Type { get; }

    public string Id { get; }

    public string Description { get; }

    public string Intent { get; }

    public IList<string> Dependencies { get; }

    public IList<KeyValuePair<string, string>> Settings { get; }

    public IList<KeyValuePair<string, string>> Metadata { get; }

    public string UnitType { get; }

    public string UnitDescription { get; }

    public string UnitDocumentationUri { get; }

    public string ModuleName { get; }

    public string ModuleType { get; }

    public string ModuleSource { get; }

    public string ModuleDescription { get; }

    public string ModuleDocumentationUri { get; }

    public string PublishedModuleUri { get; }

    public string Version { get; }

    public bool IsLocal { get; }

    public string Author { get; }

    public string Publisher { get; }

    public bool IsPublic { get; }
}
