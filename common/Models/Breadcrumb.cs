// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;

namespace DevHome.Common.Models;

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
