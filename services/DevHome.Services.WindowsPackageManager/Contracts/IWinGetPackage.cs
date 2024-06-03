// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Windows.Storage.Streams;

namespace DevHome.Services.WindowsPackageManager.Contracts;

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
    /// Gets the package catalog name
    /// </summary>
    public string CatalogName
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
    /// Gets the installed version of the package or null if the package is not installed
    /// </summary>
    public string InstalledVersion
    {
        get;
    }

    /// <summary>
    /// Gets the default version to install
    /// </summary>
    public string DefaultInstallVersion
    {
        get;
    }

    /// <summary>
    /// Gets the list of available versions for the package
    /// </summary>
    public IReadOnlyList<string> AvailableVersions
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
    /// Gets the package publisher name
    /// </summary>
    public string PublisherName
    {
        get;
    }

    /// <summary>
    /// Gets the package installation notes
    /// </summary>
    public string InstallationNotes
    {
        get;
    }
}

/// <summary>
/// Record for a package unique key following a value-based equality semantics
/// </summary>
/// <remarks>
/// A package id is unique in a catalog, but not across catalogs. To globally
/// identify a package, use a composite key of package id and catalog id.
/// </remarks>
/// <param name="packageId">Package id</param>
/// <param name="catalogId">Catalog id</param>
public record class PackageUniqueKey(string PackageId, string CatalogId);
