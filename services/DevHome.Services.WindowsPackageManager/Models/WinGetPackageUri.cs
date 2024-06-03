// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Windows package manager (winget) package Uri
/// </summary>
public class WinGetPackageUri
{
    /// <summary>
    /// Windows package manager custom protocol scheme
    /// </summary>
    private const string Scheme = "x-ms-winget";

    /// <summary>
    /// Gets the catalog name
    /// </summary>
    public string CatalogName { get; private set; }

    /// <summary>
    /// Gets the package id
    /// </summary>
    public string PackageId { get; private set; }

    /// <summary>
    /// Gets the package options
    /// </summary>
    public WinGetPackageUriOptions Options { get; private set; }

    public WinGetPackageUri(string packageStringUri)
    {
        if (!ValidUriStructure(packageStringUri, out var packageUri))
        {
            throw new UriFormatException($"Invalid winget package string uri {packageStringUri}");
        }

        // Create instance from Uri
        InitializeFromUri(packageUri);
    }

    public WinGetPackageUri(string catalogName, string packageId, WinGetPackageUriOptions options = null)
    {
        // Create intermediate Uri
        var uriString = CreateValidWinGetPackageUriString(catalogName, packageId, options ?? new(), WinGetPackageUriParameters.All);
        var uri = new Uri(uriString);

        // Create instance from Uri
        InitializeFromUri(uri);
    }

    private WinGetPackageUri(Uri packageUri)
    {
        // Private constructor expects a valid Uri
        Debug.Assert(ValidUriStructure(packageUri), $"Expected a valid winget package Uri {packageUri}");
        InitializeFromUri(packageUri);
    }

    /// <summary>
    /// Create a package Uri from a Uri
    /// </summary>
    /// <param name="uri">Uri</param>
    /// <param name="packageUri">Output package Uri</param>
    /// <returns>True if the Uri is a valid winget package Uri</returns>
    public static bool TryCreate(Uri uri, out WinGetPackageUri packageUri)
    {
        // Ensure the Uri is a WinGet Uri
        if (ValidUriStructure(uri))
        {
            packageUri = new(uri);
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
        // Ensure the Uri is a WinGet Uri
        if (ValidUriStructure(stringUri, out var uri))
        {
            packageUri = new(uri);
            return true;
        }

        packageUri = null;
        return false;
    }

    /// <summary>
    /// Generate a string Uri from the package Uri
    /// </summary>
    /// <param name="includeParameters">Include parameters</param>
    /// <returns>Uri string</returns>
    public string ToString(WinGetPackageUriParameters includeParameters)
    {
        return CreateValidWinGetPackageUriString(CatalogName, PackageId, Options, includeParameters);
    }

    /// <summary>
    /// Check if the package Uri is equal to the provided string Uri
    /// </summary>
    /// <param name="stringUri">String Uri</param>
    /// <param name="includeParameters">Include parameters</param>
    /// <returns>True if the package Uri is equal to the string Uri</returns>
    public bool Equals(string stringUri, WinGetPackageUriParameters includeParameters)
    {
        return TryCreate(stringUri, out var packageUri) && Equals(packageUri, includeParameters);
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

    /// <inheritdoc />
    public override bool Equals(object obj) => Equals(obj as WinGetPackageUri, WinGetPackageUriParameters.All);

    /// <inheritdoc />
    public override string ToString() => ToString(WinGetPackageUriParameters.All);

    /// <inheritdoc />
    public override int GetHashCode() => ToString().GetHashCode();

    /// <summary>
    /// Validate the string Uri and create a Uri
    /// </summary>
    /// <param name="stringUri">String Uri</param>
    /// <param name="uri">Output Uri</param>
    /// <returns>True if the string Uri is a valid winget package Uri</returns>
    private static bool ValidUriStructure(string stringUri, out Uri uri) => Uri.TryCreate(stringUri, UriKind.Absolute, out uri) && ValidUriStructure(uri);

    /// <summary>
    /// Validate the Uri structure
    /// </summary>
    /// <param name="uri">Uri</param>
    /// <returns>True if the Uri is a valid winget package Uri</returns>
    private static bool ValidUriStructure(Uri uri) => uri != null && uri.Scheme == Scheme && uri.Segments.Length == 2;

    /// <summary>
    /// Initialize the package Uri from a valid Uri
    /// </summary>
    /// <param name="validUri">Valid package Uri</param>
    private void InitializeFromUri(Uri validUri)
    {
        Debug.Assert(ValidUriStructure(validUri), $"Expected a valid winget package Uri {validUri}");
        CatalogName = validUri.Host;
        PackageId = validUri.Segments[1];
        Options = new(validUri);
    }

    /// <summary>
    /// Create a valid Uri string
    /// </summary>
    /// <param name="catalogName">Catalog name</param>
    /// <param name="packageId">Package id</param>
    /// <param name="options">Options</param>
    /// <param name="includeParameters">Include parameters</param>
    /// <returns>Valid Uri string</returns>
    private static string CreateValidWinGetPackageUriString(string catalogName, string packageId, WinGetPackageUriOptions options, WinGetPackageUriParameters includeParameters)
    {
        var queryString = options.ToString(includeParameters);
        var uriString = $"{Scheme}://{catalogName}/{packageId}{queryString}";
        Debug.Assert(ValidUriStructure(uriString, out var _), $"Expected to generate a valid winget package Uri {uriString}");
        return uriString;
    }
}
