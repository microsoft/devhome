// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCUnitDetails
{
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
