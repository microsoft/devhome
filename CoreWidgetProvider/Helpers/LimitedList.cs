// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace CoreWidgetProvider.Helpers;

internal class LimitedList<T> : List<T>
{
    public int MaxSize
    {
        get;
    }

    public LimitedList(int maxSize)
    {
        MaxSize = maxSize;
    }

    public new void Add(T item)
    {
        base.Add(item);
        if (Count > MaxSize)
        {
            RemoveAt(0);
        }
    }
}
