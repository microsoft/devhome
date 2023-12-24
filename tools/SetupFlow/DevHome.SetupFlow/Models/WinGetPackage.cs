// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Services;
using DevHome.SetupFlow.Services.WinGet;
using Microsoft.Management.Deployment;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Model class for a Windows Package Manager package.
/// </summary>
public class WinGetPackage : IWinGetPackage
{
    public WinGetPackage(CatalogPackage package, bool requiresElevated)
    {
        // WinGetPackage constructor copies all the required data from the
        // out-of-proc COM objects over to the current process. This ensures
        // that we have this information available even if the out-of-proc COM
        // objects are no longer available (e.g. AppInstaller service is no
        // longer running).
        Id = package.Id;
        CatalogId = package.DefaultInstallVersion.PackageCatalog.Info.Id;
        CatalogName = package.DefaultInstallVersion.PackageCatalog.Info.Name;
        UniqueKey = new (Id, CatalogId);
        Name = package.Name;
        Version = package.DefaultInstallVersion.Version;
        IsInstalled = package.InstalledVersion != null;
        IsElevationRequired = requiresElevated;
        PackageUrl = GetMetadataValue(package, metadata => new Uri(metadata.PackageUrl), nameof(CatalogPackageMetadata.PackageUrl), null);
        PublisherUrl = GetMetadataValue(package, metadata => new Uri(metadata.PublisherUrl), nameof(CatalogPackageMetadata.PublisherUrl), null);
        PublisherName = GetMetadataValue(package, metadata => metadata.Publisher, nameof(CatalogPackageMetadata.Publisher), null);
        InstallationNotes = GetMetadataValue(package, metadata => metadata.InstallationNotes, nameof(CatalogPackageMetadata.InstallationNotes), null);
    }

    public string Id { get; }

    public string CatalogId { get; }

    public string CatalogName { get; }

    public PackageUniqueKey UniqueKey { get; }

    public string Name { get; }

    public string Version { get; }

    public bool IsInstalled { get; }

    public IRandomAccessStream LightThemeIcon { get; set; }

    public IRandomAccessStream DarkThemeIcon { get; set; }

    public Uri PackageUrl { get; }

    public Uri PublisherUrl { get; }

    public string PublisherName { get; }

    public string InstallationNotes { get; }

    public bool IsElevationRequired { get; }

    public InstallPackageTask CreateInstallTask(
        IWindowsPackageManager wpm,
        ISetupFlowStringResource stringResource,
        WindowsPackageManagerFactory wingetFactory,
        Guid activityId) => new (wpm, stringResource, this, activityId);

    public Uri CreateUri(IWinGetProtocolParser protocolParser) => protocolParser.CreatePackageUri(this);

    /// <summary>
    /// Gets the package metadata from the current culture name (e.g. 'en-US')
    /// </summary>
    /// <typeparam name="T">Type of the return value</typeparam>
    /// <param name="metadataFunction">Function called with the package metadata as input</param>
    /// <param name="metadataFieldName">Name of the metadata field we want to get; used for logging</param>
    /// <param name="defaultValue">Default value returned if the package metadata threw an exception</param>
    /// <returns>Metadata function result or default value</returns>
    private T GetMetadataValue<T>(CatalogPackage package, Func<CatalogPackageMetadata, T> metadataFunction, string metadataFieldName, T defaultValue)
    {
        try
        {
            var locale = Thread.CurrentThread.CurrentCulture.Name;
            var metadata = package.DefaultInstallVersion.GetCatalogPackageMetadata(locale);
            return metadataFunction(metadata);
        }
        catch
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get package metadata [{metadataFieldName}] for package {package.Id}; defaulting to {defaultValue}");
            return defaultValue;
        }
    }
}
