// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using DevHome.Contracts.Services;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DevHome.Settings.ViewModels;

public partial class AboutViewModel : ObservableRecipient
{
    private string _versionDescription;

    public string VersionDescription
    {
        get => _versionDescription;
        set => SetProperty(ref _versionDescription, value);
    }

    public AboutViewModel()
    {
        _versionDescription = GetVersionDescription();
    }

    private static string GetVersionDescription()
    {
        IAppInfoService appInfoService = Application.Current.GetService<IAppInfoService>();
        var localizedAppName = appInfoService.GetAppNameLocalized();
        var version = appInfoService.GetAppVersion();

        return $"{localizedAppName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
