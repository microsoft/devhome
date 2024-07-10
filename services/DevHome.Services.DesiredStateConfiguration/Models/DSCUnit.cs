// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Models;

internal sealed class DSCUnit : IDSCUnit
{
    private const string DescriptionMetadataKey = "description";
    private const string ModuleMetadataKey = "module";
    private readonly IDSCUnitDetails _defaultDetails;
    private Task<IDSCUnitDetails> _loadDetailsTask;

    public string Type { get; }

    public string Id { get; }

    public Guid InstanceId { get; }

    public string Description { get; }

    public string Intent { get; }

    public string ModuleName { get; }

    public IList<string> Dependencies { get; }

    public IList<KeyValuePair<string, string>> Settings { get; }

    public IList<KeyValuePair<string, string>> Metadata { get; }

    public DSCUnit(ConfigurationUnit unit)
    {
        // Constructor copies all the required data from the out-of-proc COM
        // objects over to the current process. This ensures that we have this
        // information available even if the out-of-proc COM objects are no
        // longer available (e.g. AppInstaller service is no longer running).
        Type = unit.Type;
        Id = unit.Identifier;
        InstanceId = unit.InstanceIdentifier;
        Intent = unit.Intent.ToString();
        Dependencies = [.. unit.Dependencies];

        // Get description from settings
        unit.Metadata.TryGetValue(DescriptionMetadataKey, out var descriptionObj);
        Description = descriptionObj?.ToString() ?? string.Empty;

        // Load dictionary values into list of key value pairs
        Settings = unit.Settings.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString())).ToList();
        Metadata = unit.Metadata.Select(m => new KeyValuePair<string, string>(m.Key, m.Value.ToString())).ToList();

        // Get module name from metadata
        ModuleName = Metadata.FirstOrDefault(m => m.Key == ModuleMetadataKey).Value?.ToString() ?? string.Empty;

        // Build default details
        _defaultDetails = unit.Details == null ? new DSCUnitDetails(ModuleName) : new DSCUnitDetails(unit.Details);
        _loadDetailsTask = Task.FromResult(_defaultDetails);
    }

    /// <inheritdoc/>
    public async Task<IDSCUnitDetails> GetDetailsAsync()
    {
        var loadedDetails = await _loadDetailsTask;
        return loadedDetails ?? _defaultDetails;
    }

    /// <summary>
    /// Set an asynchronous task to load the details for the unit in the background.
    /// </summary>
    /// <param name="loadDetailsTask">Task to load the details</param>
    public void SetLoadDetailsTask(Task<IDSCUnitDetails> loadDetailsTask)
    {
        _loadDetailsTask = loadDetailsTask;
    }
}
