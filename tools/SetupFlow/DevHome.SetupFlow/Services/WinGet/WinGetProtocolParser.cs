// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using WPMPackageCatalog = Microsoft.Management.Deployment.PackageCatalog;

namespace DevHome.SetupFlow.Services.WinGet;

public record class WinGetProtocolParserResult(string packageId, WPMPackageCatalog catalog);

/// <summary>
/// Winget protocol parser
/// Protocol scheme: x-ms-winget://[catalog]/[packageId]
/// </summary>
public class WinGetProtocolParser : IWinGetProtocolParser
{
    private readonly IWinGetCatalogConnector _catalogConnector;

    public WinGetProtocolParser(IWinGetCatalogConnector catalogConnector)
    {
        _catalogConnector = catalogConnector;
    }

    /// <summary>
    /// Windows package manager custom protocol scheme
    /// </summary>
    public const string Scheme = "x-ms-winget";

    /// <summary>
    /// Reserved name for the WinGet catalog
    /// </summary>
    public const string WingetCatalogURIName = "winget";

    /// <summary>
    /// Reserved name for the Microsoft Store catalog
    /// </summary>
    public const string MsStoreCatalogURIName = "msstore";

    /// <inheritdoc/>
    public async Task<WinGetProtocolParserResult> ParseAsync(Uri packageUri)
    {
        if (packageUri.Scheme == Scheme && packageUri.Segments.Length == 2)
        {
            var packageId = packageUri.Segments[1];

            // 'winget' catalog
            if (packageUri.Host == WingetCatalogURIName)
            {
                return new (packageId, await _catalogConnector.GetPredefinedWingetCatalogAsync());
            }

            // 'msstore' catalog
            if (packageUri.Host == MsStoreCatalogURIName)
            {
                return new (packageId, await _catalogConnector.GetPredefinedMsStoreCatalogAsync());
            }

            // custom catalog
            return new (packageId, await _catalogConnector.GetCustomPackageCatalogAsync(packageUri.Host));
        }

        return null;
    }
}
