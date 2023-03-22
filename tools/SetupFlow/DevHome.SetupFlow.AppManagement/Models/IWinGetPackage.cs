// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Services;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.AppManagement.Models;

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
    /// Gets or sets the package's light icon
    /// </summary>
    public IRandomAccessStream LightThemeIcon
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the package's dark icon
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
    /// Install this package
    /// </summary>
    /// <param name="wpm">Windows package manager service</param>
    Task InstallAsync(IWindowsPackageManager wpm);
}
