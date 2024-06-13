// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCUnitDetails
{
    /// <summary>
    /// Gets the type of the configuration unit.
    /// </summary>
    public string UnitType { get; }

    /// <summary>
    /// Gets a description of the configuration unit.
    /// </summary>
    public string UnitDescription { get; }

    /// <summary>
    /// Gets the URI of the documentation for the unit of configuration.
    /// </summary>
    public string UnitDocumentationUri { get; }

    /// <summary>
    /// Gets the name of the module containing the unit of configuration.
    /// </summary>
    public string ModuleName { get; }

    /// <summary>
    /// Gets the type of the module containing the unit of configuration.
    /// </summary>
    public string ModuleType { get; }

    /// <summary>
    /// Gets the source of the module containing the unit of configuration.
    /// </summary>
    public string ModuleSource { get; }

    /// <summary>
    /// Gets the description of the module containing the unit of configuration.
    /// </summary>
    public string ModuleDescription { get; }

    /// <summary>
    /// Gets the URI of the documentation for the module containing the unit of configuration.
    /// </summary>
    public string ModuleDocumentationUri { get; }

    /// <summary>
    /// Gets the URI for the published module containing the unit of configuration.
    /// </summary>
    public string PublishedModuleUri { get; }

    /// <summary>
    /// Gets the version of the module containing the unit of configuration.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a value indicating whether the module is already present on the system.
    /// </summary>
    public bool IsLocal { get; }

    /// <summary>
    /// Gets the author of the module containing the unit of configuration.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets the publisher of the module containing the unit of configuration.
    /// </summary>
    public string Publisher { get; }

    /// <summary>
    /// Gets a value indicating whether the module comes from a public repository.
    /// </summary>
    public bool IsPublic { get; }
}
