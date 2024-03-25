﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Helpers;
using DevHome.Common.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace DevHome.Common.Behaviors;

public class BreadcrumbNavigationBehavior : Behavior<BreadcrumbBar>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ItemClicked += OnClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.ItemClicked -= OnClick;
    }

    private void OnClick(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        var crumb = args.Item as Breadcrumb;
        crumb?.NavigateTo();

        if (crumb == null)
        {
            Log.Logger()?.ReportError("BreadcrumbNavigationBehavior", "BreadcrumbBarItemClickedEventArgs.Item is not a Breadcrumb");
        }
    }
}
