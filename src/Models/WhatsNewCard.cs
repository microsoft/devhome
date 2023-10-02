// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Windows.Web.AtomPub;

namespace DevHome.Models;
public class WhatsNewCard
{
    public int Priority
    {
        get; set;
    }

    public string? Title
    {
        get; set;
    }

    public string? Description
    {
        get; set;
    }

    public string? LightThemeImage
    {
        get; set;
    }

    public string? DarkThemeImage
    {
        get; set;
    }

    public string? LightThemeImageBig
    {
        get; set;
    }

    public string? DarkThemeImageBig
    {
        get; set;
    }

    public string? Button
    {
        get; set;
    }

    public string? PageKey
    {
        get; set;
    }

    public string? Link
    {
        get; set;
    }

    public bool? ShouldShowLink
    {
        get; set;
    }

    public bool? IsBig
    {
        get; set;
    }
}
