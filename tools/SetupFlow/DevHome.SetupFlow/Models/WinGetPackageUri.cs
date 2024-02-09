// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Windows package manager (winget) package Uri
/// </summary>
/// <param name="catalogName">Catalog name</param>
/// <param name="packageId">Package Id</param>
/// <param name="options">Uri options</param>
public class WinGetPackageUri
{
    private WinGetPackageUri(string catalogName, string packageId, WinGetPackageUriOptions options = null)
    {
        CatalogName = catalogName;
        PackageId = packageId;
        Options = options ?? new();
    }

    /// <summary>
    /// Windows package manager custom protocol scheme
    /// </summary>
    private const string Scheme = "x-ms-winget";

    public string CatalogName { get; }

    public string PackageId { get; }

    public WinGetPackageUriOptions Options { get; }

    /// <summary>
    /// Create a package Uri from a Uri
    /// </summary>
    /// <param name="uri">Uri</param>
    /// <param name="packageUri">Output package Uri</param>
    /// <returns>True if the Uri is a valid winget package Uri</returns>
    public static bool TryCreate(Uri uri, out WinGetPackageUri packageUri)
    {
        // Ensure the Uri is not null
        if (uri == null)
        {
            packageUri = null;
            return false;
        }

        // Ensure the Uri is a WinGet Uri
        if (uri.Scheme == Scheme && uri.Segments.Length == 2)
        {
            var packageId = uri.Segments[1];
            var catalogUriName = uri.Host;
            WinGetPackageUriOptions packageUriOptions = new(uri.Query);
            packageUri = new(catalogUriName, packageId, packageUriOptions);
            return true;
        }

        packageUri = null;
        return false;
    }

    /// <summary>
    /// Create a package Uri from a string
    /// </summary>
    /// <param name="stringUri">String Uri</param>
    /// <param name="packageUri">Output package Uri</param>
    /// <returns>True if the string Uri is a valid winget package Uri</returns>
    public static bool TryCreate(string stringUri, out WinGetPackageUri packageUri)
    {
        // Ensure the string is a valid Uri
        packageUri = null;
        return Uri.TryCreate(stringUri, UriKind.Absolute, out var uri) && TryCreate(uri, out packageUri);
    }

    /// <summary>
    /// Create a package Uri from a string
    /// </summary>
    /// <param name="includeParameters">Include parameters</param>
    /// <returns>Uri string</returns>
    public string ToString(WinGetPackageUriParameters includeParameters)
    {
        var queryString = Options.ToString(includeParameters);
        return $"{Scheme}://{CatalogName}/{PackageId}{queryString}";
    }

    /// <summary>
    /// Check if the package Uri is equal to the provided string Uri
    /// </summary>
    /// <param name="stringUri">String Uri</param>
    /// <param name="includeParameters">Include parameters</param>
    /// <returns>True if the package Uri is equal to the string Uri</returns>
    public bool Equals(string stringUri, WinGetPackageUriParameters includeParameters)
    {
        return TryCreate(stringUri, out var uri) && Equals(uri, includeParameters);
    }

    /// <summary>
    /// Check if the package Uri is equal to the provided package Uri
    /// </summary>
    /// <param name="packageUri">Package Uri</param>
    /// <param name="includeParameters">Include parameters</param>
    /// <returns>True if the package Uri is equal to the Uri</returns>
    public bool Equals(WinGetPackageUri packageUri, WinGetPackageUriParameters includeParameters)
    {
        if (packageUri == null)
        {
            return false;
        }

        return CatalogName == packageUri.CatalogName &&
            PackageId == packageUri.PackageId &&
            Options.Equals(packageUri.Options, includeParameters);
    }

    public override bool Equals(object obj) => Equals(obj as WinGetPackageUri, WinGetPackageUriParameters.All);

    public override string ToString() => ToString(WinGetPackageUriParameters.All);

    public override int GetHashCode() => ToString().GetHashCode();
}
