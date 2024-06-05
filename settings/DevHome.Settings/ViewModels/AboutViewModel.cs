// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Extensions;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Microsoft.UI.Xaml;
using Serilog;
using Windows.Storage;
using Windows.System;

namespace DevHome.Settings.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    [ObservableProperty]
    private string _versionDescription;

    public AboutViewModel()
    {
        _versionDescription = GetVersionDescription();

        var stringResource = new StringResource("DevHome.Settings.pri", "DevHome.Settings/Resources");
        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new(stringResource.GetLocalized("Settings_Header"), typeof(SettingsViewModel).FullName!),
            new(stringResource.GetLocalized("Settings_About_Header"), typeof(AboutViewModel).FullName!),
        };
    }

    [RelayCommand]
    private async Task OpenThirdPartyNoticeAsync()
    {
        try
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/NOTICE.txt"));
            await Launcher.LaunchFileAsync(file);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to launch third party notice file. Error: {ex}");
        }
    }

    private static string GetVersionDescription()
    {
        var appInfoService = Application.Current.GetService<IAppInfoService>();
        var version = appInfoService.GetAppVersion();

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
