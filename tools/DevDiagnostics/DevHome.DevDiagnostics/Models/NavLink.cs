// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.DevDiagnostics.Models;

public class NavLink
{
    public string IconText { get; internal set; }

    public string ContentText { get; internal set; }

    public NavLink(string icon, string title)
    {
        IconText = icon;
        ContentText = title;
    }
}

public class PageNavLink : NavLink
{
    public Type? PageViewModel { get; internal set; }

    public PageNavLink(string icon, string title, Type? pageViewModel)
        : base(icon, title)
    {
        PageViewModel = pageViewModel;
    }
}
