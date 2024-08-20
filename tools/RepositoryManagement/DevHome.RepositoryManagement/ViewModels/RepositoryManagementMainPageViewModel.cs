// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DevHome.RepositoryManagement.Services;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly RepositoryManagementDataAccessService _dataAccessService;

    public ObservableCollection<RepositoryManagementItemViewModel> Items => new(_dataAccessService.GetRepositories(true));

    [RelayCommand]
    public void AddExistingRepository()
    {
    }

    public RepositoryManagementMainPageViewModel(RepositoryManagementDataAccessService dataAccessService)
    {
        _dataAccessService = dataAccessService;
    }
}
