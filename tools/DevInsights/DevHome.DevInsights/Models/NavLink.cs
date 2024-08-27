// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.DevInsights.Models;

public class NavLink
{
    public string Name { get; internal set; }

    public string IconText { get; internal set; }

    public string ContentText { get; internal set; }

    public NavLink(string name, string icon, string title)
    {
        Name = name;
        IconText = icon;
        ContentText = title;
    }
}

public class PageNavLink : NavLink
{
    public Type? PageViewModel { get; internal set; }

    public PageNavLink(string name, string icon, string title, Type? pageViewModel)
        : base(name, icon, title)
    {
        PageViewModel = pageViewModel;
    }
}
