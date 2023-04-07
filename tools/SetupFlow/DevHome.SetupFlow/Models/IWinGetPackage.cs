// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using DevHome.SetupFlow.ComInterop.Projection.WindowsPackageManager;
using DevHome.SetupFlow.Services;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Record for a package unique key following a value-based equality semantics
/// </summary>
/// <remarks>
/// A package id is unique in a catalog, but not across catalogs. To globally
/// identify a package, use a composite key of package id and catalog id.
/// </remarks>
/// <param name="packageId">Package id</param>
/// <param name="catalogId">Catalog id</param>
public record class PackageUniqueKey(string packageId, string catalogId);

/// <summary>
/// Interface for a winget package.
/// </summary>
public interface IWinGetPackage
{
    /// <summary>
    /// Gets the package Id
    /// </summary>
    public string Id
    {
        get;
    }

    /// <summary>
    /// Gets the package catalog Id
    /// </summary>
    public string CatalogId
    {
        get;
    }

    /// <summary>
    /// Gets a globally unique key for the package.
    /// </summary>
    /// <remarks>
    /// This property can be used as a key in a dictionary, hashset, etc ...
    /// </remarks>
    public PackageUniqueKey UniqueKey
    {
        get;
    }

    /// <summary>
    /// Gets the package display name
    /// </summary>
    public string Name
    {
        get;
    }

    /// <summary>
    /// Gets the version of the package which could be of any format supported
    /// by WinGet package manager (e.g. alpha-numeric, 'Unknown', '1-preview, etc...).
    /// <seealso cref="https://github.com/microsoft/winget-cli/blob/master/src/Microsoft.Management.Deployment/PackageManager.idl"/>
    /// </summary>
    public string Version
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether the package is installed
    /// </summary>
    public bool IsInstalled
    {
        get;
    }

    /// <summary>
    /// Gets or sets the package's light theme icon
    /// </summary>
    public IRandomAccessStream LightThemeIcon
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the package's dark theme icon
    /// </summary>
    public IRandomAccessStream DarkThemeIcon
    {
        get; set;
    }

    /// <summary>
    /// Gets the package url
    /// </summary>
    public Uri PackageUrl
    {
        get;
    }

    /// <summary>
    /// Gets the publisher url
    /// </summary>
    public Uri PublisherUrl
    {
        get;
    }

    /// <summary>
    /// Create an install task for this package
    /// </summary>
    /// <param name="logger">Logger service</param>
    /// <param name="wpm">Windows package manager service</param>
    /// <param name="stringResource">String resource service</param>
    /// <param name="wingetFactory">WinGet factory</param>
    /// <returns>Task object for installing this package</returns>
    InstallPackageTask CreateInstallTask(
        IWindowsPackageManager wpm,
        ISetupFlowStringResource stringResource,
        WindowsPackageManagerFactory wingetFactory);
}
