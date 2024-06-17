// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common.Behaviors;
using DevHome.Common.Models;
using DevHome.Common.Views;
using Microsoft.UI.Xaml.Data;

namespace DevHome.Common;

public abstract class ToolPage : DevHomePage
{
    public abstract string DisplayName { get; }

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ToolPage()
    {
        Breadcrumbs = [new(DisplayName, string.Empty)];
        SetBinding(NavigationViewHeaderBehavior.HeaderContextProperty, new Binding
        {
            Source = Breadcrumbs,
            Mode = BindingMode.OneWay,
        });
    }
}
