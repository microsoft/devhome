// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.Services;

/// <summary>
/// Class for loading package catalogs from a JSON data source
/// </summary>
public class WinGetPackageJsonDataSource
{
    /// <summary>
    /// Class for deserializing a JSON package catalog with package ids from
    /// winget
    /// </summary>
    private class JsonWinGetPackageCatalog
    {
        public string NameResourceKey { get; set; }

        public string DescriptionResourceKey { get; set; }

        public IList<string> WinGetPackageIds { get; set; }
    }

    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private readonly IWindowsPackageManager _wpm;

    public WinGetPackageJsonDataSource(ILogger logger, ISetupFlowStringResource stringResource, IWindowsPackageManager wpm)
    {
        _logger = logger;
        _stringResource = stringResource;
        _wpm = wpm;
    }

    /// <summary>
    /// Load package catalogs from the JSON file input
    /// </summary>
    /// <param name="fileName">JSON file name</param>
    /// <returns>List of package catalogs</returns>
    public async Task<IList<PackageCatalog>> LoadCatalogsAsync(string fileName)
    {
        // Open and deserialize JSON file
        using var fileStream = File.OpenRead(fileName);
        var result = new List<PackageCatalog>();
        var options = new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip };
        var jsonCatalogList = await JsonSerializer.DeserializeAsync<IList<JsonWinGetPackageCatalog>>(fileStream, options);

        foreach (var jsonCatalog in jsonCatalogList)
        {
            var packageCatalog = await LoadCatalogAsync(jsonCatalog);
            if (packageCatalog?.Packages.Any() ?? false)
            {
                result.Add(packageCatalog);
            }
        }

        return result;
    }

    /// <summary>
    /// Load a package catalog with the list of winget packages sorted based on
    /// the input JSON catalog
    /// </summary>
    /// <param name="jsonCatalog">JSON catalog</param>
    /// <returns>Package catalog</returns>
    private async Task<PackageCatalog> LoadCatalogAsync(JsonWinGetPackageCatalog jsonCatalog)
    {
        try
        {
            // Get packages from winget catalog
            var unorderedPackages = await _wpm.WinGetCatalog.GetPackagesAsync(jsonCatalog.WinGetPackageIds.ToHashSet());

            // Sort result based on the input
            var unorderedPackagesMap = unorderedPackages.ToDictionary(p => p.Id, p => p);
            var orderedPackages = jsonCatalog.WinGetPackageIds
                .Select(id => unorderedPackagesMap.GetValueOrDefault(id, null))
                .Where(package => package is not null);

            return new PackageCatalog()
            {
                Name = _stringResource.GetLocalized(jsonCatalog.NameResourceKey),
                Description = _stringResource.GetLocalized(jsonCatalog.DescriptionResourceKey),
                Packages = orderedPackages.ToReadOnlyCollection(),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetPackageJsonDataSource), LogLevel.Info, $"Error loading packages from winget catalog: {e.Message}");
            return null;
        }
    }
}
