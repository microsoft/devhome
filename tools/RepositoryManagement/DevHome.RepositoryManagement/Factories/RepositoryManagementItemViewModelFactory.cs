// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.Database.Services;
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

    private readonly IStringResource _stringResource;

    private readonly ConfigurationFileBuilder _configurationFileBuilder;

    public RepositoryManagementItemViewModelFactory(
        Window window,
        RepositoryManagementDataAccessService dataAccess,
        IStringResource stringResource,
        ConfigurationFileBuilder configurationFileBuilder)
    {
        _window = window;
        _dataAccessService = dataAccess;
        _stringResource = stringResource;
        _configurationFileBuilder = configurationFileBuilder;
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

        var newViewModel = new RepositoryManagementItemViewModel(_window, _dataAccessService, _stringResource, _configurationFileBuilder, localRepositoryName, localCloneLocation);

        newViewModel.IsHiddenFromPage = localIsHidden;

        return newViewModel;
    }
}
