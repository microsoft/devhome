// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common;

public class RepositoryClonedEventArgs
{
    public string? RepositoryName
    {
        get; set;
    }

    public string? CloneLocation
    {
        get; set;
    }

    public IRepository? Repository
    {
        get; set;
    }
}

public class Eventing
{
    public event EventHandler<RepositoryClonedEventArgs>? RepositoryCloned;

    public IEnumerable<object> Seen => _seenEvents;

    private List<object> _seenEvents = new List<object>();

    public void OnRepositoryCloned(RepositoryClonedEventArgs args)
    {
        RepositoryCloned?.Invoke(this, args);
        _seenEvents.Add(args);
    }
}
