// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using DevHome.SetupFlow.AppManagement.Services;

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
    /// Gets the package catalog id
    /// </summary>
    public string CatalogId
    {
        get;
    }

    /// <summary>
    /// Gets a globally unique composite key for the package.
    /// This property can be used as a key in a dictionary, hashset, etc ...
    /// </summary>
    public (string, string) CompositeKey
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
    /// Gets the package image uri
    /// </summary>
    public Uri ImageUri
    {
        get;
    }

    /// <summary>
    /// Gets the package "learn more" uri
    /// </summary>
    public Uri PackageUri
    {
        get;
    }

    /// <summary>
    /// Install this package
    /// </summary>
    /// <param name="wpm">Windows package manager service</param>
    Task InstallAsync(IWindowsPackageManager wpm);

    InstallPackageTask CreateInstallTask(IWindowsPackageManager wpm);
}
