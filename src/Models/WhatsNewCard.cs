// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

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

    public string? ButtonText
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

    public bool ShouldShowLink { get; set; } = true;

    public bool? ShouldShowIcon
    {
        get; set;
    }

    public bool? IsBig
    {
        get; set;
    }

    public Visibility HasLinkAndShouldShowIt(string? link, bool shouldShowLink)
    {
        if (link != null && shouldShowLink)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
    }
}
