// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace DevHome.Common.Extensions;

public static class ObservableCollectionExtensions
{
    public static void Add<T>(this ObservableCollection<T> collection, T item, Func<T, IComparable> keySelector)
    {
        collection.Add(item);
        var sortedList = collection.OrderBy(keySelector).ToList();
        collection.Clear();
        foreach (var sortedItem in sortedList)
        {
            collection.Add(sortedItem);
        }
    }
}
