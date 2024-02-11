// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Web;

namespace DevHome.SetupFlow.Models;

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

    public string Version { get; }

    public bool VersionSpecified => !string.IsNullOrWhiteSpace(Version);

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

    public override string ToString() => ToString(WinGetPackageUriParameters.All);

    public override bool Equals(object obj) => Equals(obj as WinGetPackageUriOptions, WinGetPackageUriParameters.All);

    public override int GetHashCode() => ToString().GetHashCode();
}
