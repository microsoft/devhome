// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Database;
using DevHome.RepositoryManagement.Services;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementMainPageViewModel
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementMainPageViewModel));

    private readonly IHost _host;

    private readonly List<RepositoryManagementItemViewModel> _items;

    public ObservableCollection<RepositoryManagementItemViewModel> Items => RefreshItemsForUI();

    [RelayCommand]
    public void AddExistingRepository()
    {
    }

    public RepositoryManagementMainPageViewModel(IHost host)
    {
        _items = new List<RepositoryManagementItemViewModel>();
        _host = host;

        var items = _host.GetService<RepositoryManagementDataAccessService>().GetRepositories();

        _items.AddRange(items);
    }

    private ObservableCollection<RepositoryManagementItemViewModel> RefreshItemsForUI()
    {
        return new(_items);
    }
}
