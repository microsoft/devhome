// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.ViewModels;

public abstract class BreadcrumbViewModel : ObservableObject
{
    public abstract ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
    }
}
