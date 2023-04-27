// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.Models;

public class Breadcrumb
{
    public Breadcrumb(string label, string page)
    {
        Label = label;
        Page = page;
    }

    public string Label { get; }

    public string Page { get; }

    public override string ToString() => Label;

    public void NavigateTo()
    {
        var navigationService = Application.Current.GetService<INavigationService>();
        navigationService.NavigateTo(Page);
    }
}
