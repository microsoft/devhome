// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using DevHome.Settings.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace DevHome.Settings.Views;

public sealed partial class ExperimentalFeaturesPage : Page
{
    public ExperimentalFeaturesViewModel ViewModel
    {
        get;
    }

    private readonly ILocalSettingsService _localSettingsService;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public ExperimentalFeaturesPage()
    {
        ViewModel = Application.Current.GetService<ExperimentalFeaturesViewModel>();
        _localSettingsService = Application.Current.GetService<ILocalSettingsService>();
        this.InitializeComponent();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new Breadcrumb(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new Breadcrumb(stringResource.GetLocalized("Settings_ExperimentalFeatures_Header"), typeof(ExperimentalFeaturesViewModel).FullName!),
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
