// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.ViewModels;

internal interface IBreadcrumbViewModel
{
    public abstract ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    internal void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
    }
}
