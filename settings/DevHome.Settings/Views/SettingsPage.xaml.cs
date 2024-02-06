// Copyright (c) Microsoft Corporation..
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DevHome.Settings.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = new SettingsViewModel();
        this.InitializeComponent();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
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
