// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Web;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Windows package manager (winget) package Uri options
/// </summary>
public sealed class WinGetPackageUriOptions
{
    // Query parameter names
    private const string VersionQueryParameter = "version";

    internal WinGetPackageUriOptions(string packageUriQuery = null)
    {
        var queryParams = HttpUtility.ParseQueryString(packageUriQuery ?? string.Empty);
        Version = queryParams.Get(VersionQueryParameter) ?? string.Empty;
    }

    public string Version { get; }

    public string ToString(WinGetPackageUriParameters includeParameters)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);

        // Add version
        if (includeParameters.HasFlag(WinGetPackageUriParameters.Version) && !string.IsNullOrWhiteSpace(Version))
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
