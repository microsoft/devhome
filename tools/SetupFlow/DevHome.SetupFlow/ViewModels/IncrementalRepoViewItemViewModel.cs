// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.Collections;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.ViewModels;

public class IncrementalRepoViewItemViewModel : IIncrementalSource<RepoViewListItem>
{
    private readonly List<RepoViewListItem> _items;

    public IncrementalRepoViewItemViewModel()
    {
        _items = new List<RepoViewListItem>();
    }

    public IncrementalRepoViewItemViewModel(List<RepoViewListItem> items)
    {
        _items = items;
    }

    public async Task<IEnumerable<RepoViewListItem>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        // Gets items from the collection according to pageIndex and pageSize parameters.
        IEnumerable<RepoViewListItem> reposToReturn = new List<RepoViewListItem>();
        await Task.Run(
            () =>
        {
            reposToReturn = (from p in _items
                          select p).Skip(pageIndex * pageSize).Take(pageSize);
        },
            cancellationToken);

        return reposToReturn;
    }
}
