// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;

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
        var version = appInfoService.GetAppVersion();

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
