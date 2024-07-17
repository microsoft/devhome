// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Models;

internal sealed class DSCUnit : IDSCUnit
{
    private const string DescriptionMetadataKey = "description";
    private const string ModuleMetadataKey = "module";

    public DSCUnit(ConfigurationUnit unit)
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

        // Load dictionary values into list of key value pairs
        Settings = unit.Settings.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString())).ToList();
        Metadata = unit.Metadata.Select(m => new KeyValuePair<string, string>(m.Key, m.Value.ToString())).ToList();

        // Load details if available, otherwise create empty details with just
        // the module name if available
        if (unit.Details == null)
        {
            // Get module name from metadata
            unit.Metadata.TryGetValue(ModuleMetadataKey, out var moduleObj);
            Details = new DSCUnitDetails(moduleObj?.ToString() ?? string.Empty);
        }
        else
        {
            Details = new DSCUnitDetails(unit.Details);
        }
    }

    public string Type { get; }

    public string Id { get; }

    public string Description { get; }

    public string Intent { get; }

    public IList<string> Dependencies { get; }

    public IList<KeyValuePair<string, string>> Settings { get; }

    public IList<KeyValuePair<string, string>> Metadata { get; }

    public IDSCUnitDetails Details { get; }
}
