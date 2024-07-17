// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Customization.Views;

public sealed partial class MainPage : ToolPage
{
    public override string ShortName => "Windows customization";

    public MainPageViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get;
    }

    public MainPage()
    {
        ViewModel = Application.Current.GetService<MainPageViewModel>();
        InitializeComponent();

        var stringResource = new StringResource("DevHome.Customization/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
        };
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < Breadcrumbs.Count - 1)
        {
            var crumb = (Breadcrumb)args.Item;
            crumb.NavigateTo();
        }
    }
}
