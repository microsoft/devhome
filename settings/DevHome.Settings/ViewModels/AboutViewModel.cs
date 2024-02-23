// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.Models;
using Microsoft.UI.Xaml;

namespace DevHome.Settings.ViewModels;

public partial class AboutViewModel : BreadcrumbViewModel
{
    [ObservableProperty]
    private string _versionDescription;

    public override ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public AboutViewModel()
    {
        _versionDescription = GetVersionDescription();

        var stringResource = new StringResource("DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_About_Header"), typeof(AboutViewModel).FullName!),
        };
    }

    private static string GetVersionDescription()
    {
        var appInfoService = Application.Current.GetService<IAppInfoService>();
        var version = appInfoService.GetAppVersion();

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
