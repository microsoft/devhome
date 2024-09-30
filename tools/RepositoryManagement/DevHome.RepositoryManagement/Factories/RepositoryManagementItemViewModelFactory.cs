// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Services;
using DevHome.Database.Services;
using DevHome.RepositoryManagement.Services;
using DevHome.RepositoryManagement.ViewModels;
using DevHome.SetupFlow.Services;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.RepositoryManagement.Factories;

public class RepositoryManagementItemViewModelFactory
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementItemViewModelFactory));

    private readonly Window _window;

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    private readonly IExtensionService _extensionService;

    private readonly RepositoryEnhancerService _repositoryEnhancerService;

    public RepositoryManagementItemViewModelFactory(
        Window window,
        RepositoryManagementDataAccessService dataAccess,
        ConfigurationFileBuilder configurationFileBuilder,
        IExtensionService extensionService,
        RepositoryEnhancerService repositoryEnhancerService)
    {
        _window = window;
        _dataAccessService = dataAccess;
        _configurationFileBuilder = configurationFileBuilder;
        _extensionService = extensionService;
        _repositoryEnhancerService = repositoryEnhancerService;
    }

    public RepositoryManagementItemViewModel MakeViewModel(string repositoryName, string cloneLocation, bool isHidden)
    {
        var localIsHidden = isHidden;
        var localRepositoryName = repositoryName;
        if (string.IsNullOrEmpty(repositoryName))
        {
            _log.Warning($"{nameof(repositoryName)} is either null or empty.  Hiding repository");
            localRepositoryName = string.Empty;
            localIsHidden = true;
        }

        var localCloneLocation = cloneLocation;
        if (string.IsNullOrEmpty(cloneLocation))
        {
            _log.Warning($"{nameof(cloneLocation)} is either null or empty.  Hiding repository");
            localCloneLocation = string.Empty;
            localIsHidden = true;
        }

        var newViewModel = new RepositoryManagementItemViewModel(
            _window,
            _dataAccessService,
            _configurationFileBuilder,
            _extensionService,
            _repositoryEnhancerService,
            localRepositoryName,
            localCloneLocation);

        newViewModel.IsHiddenFromPage = localIsHidden;

        return newViewModel;
    }
}
