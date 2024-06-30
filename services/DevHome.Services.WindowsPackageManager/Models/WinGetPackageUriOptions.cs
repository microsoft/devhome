// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Web;

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Windows package manager (winget) package Uri options
/// </summary>
public sealed class WinGetPackageUriOptions
{
    // Query parameter names
    private const string VersionQueryParameter = "version";

    public WinGetPackageUriOptions(string version = null)
    {
        Version = version;
    }

    internal WinGetPackageUriOptions(Uri packageUri)
    {
        var queryParams = HttpUtility.ParseQueryString(packageUri.Query);
        Version = queryParams.Get(VersionQueryParameter);
    }

    /// <summary>
    /// Gets the version of the package
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="Version"/> is specified
    /// </summary>
    public bool VersionSpecified => !string.IsNullOrWhiteSpace(Version);

    /// <summary>
    /// Returns the string representation of the options
    /// </summary>
    /// <param name="includeParameters">The parameters to include in the string</param>
    /// <returns>Options as a string</returns>
    public string ToString(WinGetPackageUriParameters includeParameters)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);

        // Add version
        if (includeParameters.HasFlag(WinGetPackageUriParameters.Version) && VersionSpecified)
        {
            queryParams.Add(VersionQueryParameter, Version);
        }

        return queryParams.Count > 0 ? $"?{queryParams}" : string.Empty;
    }

    /// <summary>
    /// Compares the options with another options
    /// </summary>
    /// <param name="options">Target options to compare</param>
    /// <param name="includeParameters">The parameters to include in the comparison</param>
    /// <returns>True if the options are equal; otherwise, false</returns>
    public bool Equals(WinGetPackageUriOptions options, WinGetPackageUriParameters includeParameters)
    {
        if (options == null)
        {
            return false;
        }

        // Check version
        if (includeParameters.HasFlag(WinGetPackageUriParameters.Version) && Version != options.Version)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => ToString(WinGetPackageUriParameters.All);

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as WinGetPackageUriOptions, WinGetPackageUriParameters.All);

    /// <inheritdoc/>
    public override int GetHashCode() => ToString().GetHashCode();
}
