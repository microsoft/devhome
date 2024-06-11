// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Models;

internal sealed class DSCUnitDetails : IDSCUnitDetails
{
    public DSCUnitDetails(IConfigurationUnitProcessorDetails details)
    {
        // Constructor copies all the required data from the out-of-proc COM
        // objects over to the current process. This ensures that we have this
        // information available even if the out-of-proc COM objects are no
        // longer available (e.g. AppInstaller service is no longer running).
        UnitType = details.UnitType;
        UnitDescription = details.UnitDescription;
        UnitDocumentationUri = details.UnitDocumentationUri?.ToString();
        ModuleName = details.ModuleName;
        ModuleType = details.ModuleType;
        ModuleSource = details.ModuleSource;
        ModuleDescription = details.ModuleDescription;
        ModuleDocumentationUri = details.ModuleDocumentationUri?.ToString();
        PublishedModuleUri = details.PublishedModuleUri?.ToString();
        Version = details.Version;
        IsLocal = details.IsLocal;
        Author = details.Author;
        Publisher = details.Publisher;
        IsPublic = details.IsPublic;
    }

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
