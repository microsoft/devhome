// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Services;
using Microsoft.Management.Deployment;

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

    public string CatalogId => _package.DefaultInstallVersion.PackageCatalog.Info.Id;

    public string Name => _package.Name;

    public string Version => _package.DefaultInstallVersion.Version;

    public Uri ImageUri
    {
        get; set;
    }

    public Uri PackageUrl => TryGetMetadataValue(metadata => new Uri(metadata.PackageUrl), null);

    public Uri PublisherUrl => TryGetMetadataValue(metadata => new Uri(metadata.PublisherUrl), null);

    public async Task InstallAsync(IWindowsPackageManager wpm)
    {
        await wpm.InstallPackageAsync(this);
    }

    private T TryGetMetadataValue<T>(Func<CatalogPackageMetadata, T> func, T defaultValue)
    {
        try
        {
            var locale = Thread.CurrentThread.CurrentCulture.Name;
            var metadata = _package.DefaultInstallVersion.GetCatalogPackageMetadata(locale);
            return func(metadata);
        }
        catch
        {
            return default;
        }
    }
}
