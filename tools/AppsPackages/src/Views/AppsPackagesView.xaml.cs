// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.AppsPackages.ViewModels;
using DevHome.Common;

namespace DevHome.AppsPackages.Views;

public partial class AppsPackagesView : ToolPage
{
    public override string ShortName => "AppsPackagesView";

    public AppsPackagesViewModel ViewModel
    {
        get;
    }

    public AppsPackagesView()
    {
        ViewModel = new AppsPackagesViewModel();
        this.InitializeComponent();
    }
}
