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
        var result = (from p in _items
                      select p).Skip(pageIndex * pageSize).Take(pageSize);

        // Simulates a longer request...
        // Make sure the list is still in order after a refresh,
        // even if the first page takes longer to load
        if (pageIndex == 0)
        {
            await Task.Delay(200, cancellationToken);
        }
        else
        {
            await Task.Delay(100, cancellationToken);
        }

        return result;
    }
}
