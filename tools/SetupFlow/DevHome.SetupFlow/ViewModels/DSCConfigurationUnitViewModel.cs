// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.SetupFlow.ViewModels;

public class DSCConfigurationUnitViewModel
{
    private readonly IDSCUnit _configurationUnit;

    public DSCConfigurationUnitViewModel(IDSCUnit configurationUnit)
    {
        _configurationUnit = configurationUnit;
    }

    public string Id => _configurationUnit.Id;

    public string Title => GetTitle();

    public string Type => _configurationUnit.Type;

    public string Description => _configurationUnit.Description;

    public string Intent => _configurationUnit.Intent;

    public IList<string> Dependencies => _configurationUnit.Dependencies;

    public IList<KeyValuePair<string, string>> Settings => _configurationUnit.Settings;

    public IList<KeyValuePair<string, string>> Metadata => _configurationUnit.Metadata;

    public string UnitType => _configurationUnit.Details?.UnitType;

    public string UnitDescription => _configurationUnit.Details?.UnitDescription;

    public string UnitDocumentationUri => _configurationUnit.Details?.UnitDocumentationUri;

    public string ModuleName => _configurationUnit.Details?.ModuleName;

    public string ModuleType => _configurationUnit.Details?.ModuleType;

    public string ModuleSource => _configurationUnit.Details?.ModuleSource;

    public string ModuleDescription => _configurationUnit.Details?.ModuleDescription;

    public string ModuleDocumentationUri => _configurationUnit.Details?.ModuleDocumentationUri;

    public string PublishedModuleUri => _configurationUnit.Details?.PublishedModuleUri;

    public string Version => _configurationUnit.Details?.Version;

    public bool IsLocal => _configurationUnit.Details?.IsLocal ?? false;

    public string Author => _configurationUnit.Details?.Author;

    public string Publisher => _configurationUnit.Details?.Publisher;

    public bool IsPublic => _configurationUnit.Details?.IsPublic ?? false;

    private string GetTitle()
    {
        if (!string.IsNullOrEmpty(Description))
        {
            return Description;
        }

        if (!string.IsNullOrEmpty(ModuleDescription))
        {
            return ModuleDescription;
        }

        return $"{ModuleName}/{Type}";
    }
}
