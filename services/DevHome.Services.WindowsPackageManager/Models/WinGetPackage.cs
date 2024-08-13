// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DevHome.Services.WindowsPackageManager.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Management.Deployment;
using Windows.Storage.Streams;

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Model class for a Windows Package Manager package.
/// </summary>
internal sealed class WinGetPackage : IWinGetPackage
{
    private readonly ILogger _logger;

    public WinGetPackage(ILogger logger, CatalogPackage package, bool requiresElevated)
    {
        // WinGetPackage constructor copies all the required data from the
        // out-of-proc COM objects over to the current process. This ensures
        // that we have this information available even if the out-of-proc COM
        // objects are no longer available (e.g. AppInstaller service is no
        // longer running).
        _logger = logger;
        Id = package.Id;
        CatalogId = GetPackageVersionInfo(package).PackageCatalog.Info.Id;
        CatalogName = GetPackageVersionInfo(package).PackageCatalog.Info.Name;
        UniqueKey = new(Id, CatalogId);
        Name = package.Name;
        AvailableVersions = package.AvailableVersions.Select(v => v.Version).ToList();
        InstalledVersion = FindVersion(package.AvailableVersions, package.InstalledVersion);
        DefaultInstallVersion = FindVersion(package.AvailableVersions, package.DefaultInstallVersion);
        IsInstalled = InstalledVersion != null;
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

    public string InstalledVersion { get; }

    public string DefaultInstallVersion { get; }

    public IReadOnlyList<string> AvailableVersions { get; }

    public bool IsInstalled { get; }

    public IRandomAccessStream LightThemeIcon { get; set; }

    public IRandomAccessStream DarkThemeIcon { get; set; }

    public Uri PackageUrl { get; }

    public Uri PublisherUrl { get; }

    public string PublisherName { get; }

    public string InstallationNotes { get; }

    public bool IsElevationRequired { get; }

    public WinGetPackageUri GetUri(string installVersion = null)
    {
        var uriOptions = string.IsNullOrEmpty(installVersion) ? null : new WinGetPackageUriOptions(installVersion);
        return new(CatalogName, Id, uriOptions);
    }

    private PackageVersionInfo GetPackageVersionInfo(CatalogPackage package)
    {
        // Pinned packages do not have a default install version set
        if (package.DefaultInstallVersion != null)
        {
            return package.DefaultInstallVersion;
        }

        if (package.AvailableVersions.Count > 0)
        {
            return package.GetPackageVersionInfo(package.AvailableVersions[0]);
        }

        throw new ArgumentException($"Package {package.Name} does not have any versions");
    }

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
            var metadata = GetPackageVersionInfo(package).GetCatalogPackageMetadata(locale);
            return metadataFunction(metadata);
        }
        catch
        {
            _logger.LogWarning($"Failed to get package metadata [{metadataFieldName}] for package {package.Id}; defaulting to {defaultValue}");
            return defaultValue;
        }
    }

    /// <summary>
    /// Find the provided version in the list of available versions
    /// </summary>
    /// <param name="availableVersions">List of available versions</param>
    /// <param name="versionInfo">Version to find</param>
    /// <returns>Package version</returns>
    private string FindVersion(IReadOnlyList<PackageVersionId> availableVersions, PackageVersionInfo versionInfo)
    {
        if (versionInfo == null)
        {
            return null;
        }

        // Best effort to find the version in the list of available versions
        // If CompareToVersion throws an exception, we default to the version provided
        try
        {
            for (var i = 0; i < availableVersions.Count; i++)
            {
                if (versionInfo.CompareToVersion(availableVersions[i].Version) == CompareResult.Equal)
                {
                    return availableVersions[i].Version;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Unable to validate if the version {versionInfo.Version} is in the list of available versions");
        }

        return versionInfo.Version;
    }
}
