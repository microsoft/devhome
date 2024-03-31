// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.ViewModels;

public class DSCConfigurationUnitViewModel
{
    private readonly DSCConfigurationUnit _configurationUnit;

    public DSCConfigurationUnitViewModel(DSCConfigurationUnit configurationUnit)
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

    public string UnitType => _configurationUnit.UnitType;

    public string UnitDescription => _configurationUnit.UnitDescription;

    public string UnitDocumentationUri => _configurationUnit.UnitDocumentationUri;

    public string ModuleName => _configurationUnit.ModuleName;

    public string ModuleType => _configurationUnit.ModuleType;

    public string ModuleSource => _configurationUnit.ModuleSource;

    public string ModuleDescription => _configurationUnit.ModuleDescription;

    public string ModuleDocumentationUri => _configurationUnit.ModuleDocumentationUri;

    public string PublishedModuleUri => _configurationUnit.PublishedModuleUri;

    public string Version => _configurationUnit.Version;

    public bool IsLocal => _configurationUnit.IsLocal;

    public string Author => _configurationUnit.Author;

    public string Publisher => _configurationUnit.Publisher;

    public bool IsPublic => _configurationUnit.IsPublic;

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
