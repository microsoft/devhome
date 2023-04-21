// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Common.WindowsPackageManager;
using DevHome.SetupFlow.Services;
using Microsoft.Management.Deployment;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Model class for a Windows Package Manager package.
/// </summary>
public class WinGetPackage : IWinGetPackage
{
    private readonly CatalogPackage _package;
    private readonly Lazy<Uri> _packageUrl;
    private readonly Lazy<Uri> _publisherUrl;
    private readonly Lazy<string> _publisherName;
    private readonly PackageUniqueKey _uniqueKey;

    public WinGetPackage(CatalogPackage package)
    {
        _package = package;
        _packageUrl = new (() => GetMetadataValue(metadata => new Uri(metadata.PackageUrl), nameof(CatalogPackageMetadata.PackageUrl), null));
        _publisherUrl = new (() => GetMetadataValue(metadata => new Uri(metadata.PublisherUrl), nameof(CatalogPackageMetadata.PublisherUrl), null));
        _publisherName = new (() => GetMetadataValue(metadata => metadata.Publisher, nameof(CatalogPackageMetadata.Publisher), null));
        _uniqueKey = new (Id, CatalogId);
    }

    public CatalogPackage CatalogPackage => _package;

    public string Id => _package.Id;

    public string CatalogId => _package.DefaultInstallVersion.PackageCatalog.Info.Id;

    public string CatalogName => _package.DefaultInstallVersion.PackageCatalog.Info.Name;

    public PackageUniqueKey UniqueKey => _uniqueKey;

    public string Name => _package.Name;

    public string Version => _package.DefaultInstallVersion.Version;

    public bool IsInstalled => _package.InstalledVersion != null;

    public IRandomAccessStream LightThemeIcon
    {
        get; set;
    }

    public IRandomAccessStream DarkThemeIcon
    {
        get; set;
    }

    public Uri PackageUrl => _packageUrl.Value;

    public Uri PublisherUrl => _publisherUrl.Value;

    public string PublisherName => _publisherName.Value;

    public InstallPackageTask CreateInstallTask(
        IWindowsPackageManager wpm,
        ISetupFlowStringResource stringResource,
        WindowsPackageManagerFactory wingetFactory) => new (wpm, stringResource, wingetFactory, this);

    /// <summary>
    /// Check if the package requires elevation
    /// </summary>
    /// <param name="options">Install options</param>
    /// <returns>True if the package requires elevation</returns>
    public bool RequiresElevation(InstallOptions options)
    {
        try
        {
            // TODO Use the API contract version to check if this method can be
            // called instead of a try/catch
            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting applicable installer info for package {Id}");
            var applicableInstaller = _package.DefaultInstallVersion.GetApplicableInstaller(options);
            if (applicableInstaller != null)
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Elevation requirement = {applicableInstaller.ElevationRequirement} for package {Id}");
                return applicableInstaller.ElevationRequirement == ElevationRequirement.ElevationRequired || applicableInstaller.ElevationRequirement == ElevationRequirement.ElevatesSelf;
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"No applicable installer info found for package {Id}; defaulting to not requiring elevation");
                return false;
            }
        }
        catch
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get elevation requirement for package {Id}; defaulting to not requiring elevation");
            return false;
        }
    }

    /// <summary>
    /// Gets the package metadata from the current culture name (e.g. 'en-US')
    /// </summary>
    /// <typeparam name="T">Type of the return value</typeparam>
    /// <param name="metadataFunction">Function called with the package metadata as input</param>
    /// <param name="metadataFieldName">Name of the metadata field we want to get; used for logging</param>
    /// <param name="defaultValue">Default value returned if the package metadata threw an exception</param>
    /// <returns>Metadata function result or default value</returns>
    private T GetMetadataValue<T>(Func<CatalogPackageMetadata, T> metadataFunction, string metadataFieldName, T defaultValue)
    {
        try
        {
            var locale = Thread.CurrentThread.CurrentCulture.Name;
            var metadata = _package.DefaultInstallVersion.GetCatalogPackageMetadata(locale);
            return metadataFunction(metadata);
        }
        catch
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get package metadata [{metadataFieldName}] for package {_package.Id}; defaulting to {defaultValue}");
            return defaultValue;
        }
    }
}
