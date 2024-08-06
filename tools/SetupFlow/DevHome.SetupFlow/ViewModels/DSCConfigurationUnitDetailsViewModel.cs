// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.SetupFlow.ViewModels;

public class DSCConfigurationUnitDetailsViewModel
{
    private readonly IDSCUnitDetails _unitDetails;

    public DSCConfigurationUnitDetailsViewModel(IDSCUnitDetails unitDetails)
    {
        _unitDetails = unitDetails;
    }

    public string UnitType => _unitDetails.UnitType;

    public string UnitDescription => _unitDetails.UnitDescription;

    public string UnitDocumentationUri => _unitDetails.UnitDocumentationUri;

    public string ModuleName => _unitDetails.ModuleName;

    public string ModuleType => _unitDetails.ModuleType;

    public string ModuleSource => _unitDetails.ModuleSource;

    public string ModuleDescription => _unitDetails.ModuleDescription;

    public string ModuleDocumentationUri => _unitDetails.ModuleDocumentationUri;

    public string PublishedModuleUri => _unitDetails.PublishedModuleUri;

    public string Version => _unitDetails.Version;

    public bool IsLocal => _unitDetails.IsLocal;

    public string Author => _unitDetails.Author;

    public string Publisher => _unitDetails.Publisher;

    public bool IsPublic => _unitDetails.IsPublic;
}
