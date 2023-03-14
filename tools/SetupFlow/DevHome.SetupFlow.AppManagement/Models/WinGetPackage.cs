// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Services;
using Microsoft.Management.Deployment;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.AppManagement.Models;

/// <summary>
/// Model class for a Windows Package Manager package.
/// </summary>
public class WinGetPackage : IWinGetPackage
{
    private readonly CatalogPackage _package;

    public WinGetPackage(CatalogPackage package)
    {
        _package = package;
    }

    public CatalogPackage CatalogPackage => _package;

    public string Id => _package.Id;

    public string Name => _package.Name;

    public string Version => _package.DefaultInstallVersion.Version;

    public IRandomAccessStream LightThemeIcon
    {
        get; set;
    }

    public IRandomAccessStream DarkThemeIcon
    {
        get; set;
    }

    public Uri PackageUri
    {
        get; set;
    }

    public async Task InstallAsync(IWindowsPackageManager wpm)
    {
        await wpm.InstallPackageAsync(this);
    }
}
