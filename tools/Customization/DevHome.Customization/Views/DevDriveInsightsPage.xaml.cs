// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;

namespace DevHome.Customization.Views;

public sealed partial class DevDriveInsightsPage : AutoFocusPage
{
    public DevDriveInsightsViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> Breadcrumbs
    {
        get;
    }

    public DevDriveInsightsPage()
    {
        ViewModel = Application.Current.GetService<DevDriveInsightsViewModel>();
        var stringResource = new StringResource("DevHome.Customization.pri", "DevHome.Customization/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("MainPage_Header"), typeof(MainPageViewModel).FullName!),
            new(stringResource.GetLocalized("DevDriveInsights_Header"), typeof(DevDriveInsightsViewModel).FullName!),
        };
        this.InitializeComponent();
    }
}
