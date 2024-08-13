// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using Microsoft.Extensions.Hosting;

namespace DevHome.RepositoryManagement.ViewModels;

public class RepositoryManagementMainPageViewModel
{
    private readonly IHost _host;

    public ObservableCollection<RepositoryManagementItemViewModel> Items { get; } = new();

    public RepositoryManagementMainPageViewModel(IHost host)
    {
        _host = host;
    }

    // Some test data to show off in the Repository Management page.
    public void PopulateTestData()
    {
        Items.Clear();
        for (var x = 0; x < 5; x++)
        {
            var listItem = _host.GetService<RepositoryManagementItemViewModel>();
            listItem.RepositoryName = $"MicrosoftRepository{x}";
            listItem.ClonePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), x.ToString(CultureInfo.InvariantCulture));
            listItem.LatestCommit = $"dhoehna * author {x} min";
            listItem.Branch = "main";
            Items.Add(listItem);
        }
    }
}
