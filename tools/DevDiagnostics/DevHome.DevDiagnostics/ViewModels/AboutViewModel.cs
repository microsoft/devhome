// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.DevDiagnostics.Helpers;
using DevHome.DevDiagnostics.Services;
using Microsoft.UI.Xaml;
using Serilog;

namespace DevHome.DevDiagnostics.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    private static readonly ILogger _log = Log.ForContext("SourceContext", nameof(AboutViewModel));

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private string _versionDescription;

    public AboutViewModel()
    {
        _versionDescription = GetVersionDescription();

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(CommonHelper.GetLocalizedString("SettingsPageHeader"), typeof(SettingsPageViewModel).FullName!),
            new(CommonHelper.GetLocalizedString("SettingsAboutHeader"), typeof(AboutViewModel).FullName!),
        };
    }

    private static string GetVersionDescription()
    {
        var appInfoService = Application.Current.GetService<DDAppInfoService>();
        var version = appInfoService.GetAppVersion();
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
