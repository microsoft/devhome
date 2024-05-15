// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.PI.Models;

public class NavLink
{
    public string IconText { get; internal set; }

    public string ContentText { get; internal set; }

    public Type? PageViewModel { get; internal set; }

    public NavLink(string i, string c, Type? pageViewModel)
    {
        IconText = i;
        ContentText = c;
        PageViewModel = pageViewModel;
    }
}
