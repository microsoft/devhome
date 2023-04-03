// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;

namespace DevHome.SetupFlow.AppManagement.Services;

/// <summary>
/// Class for loading package catalogs from a JSON data source
/// </summary>
public class WinGetPackageJsonDataSource : WinGetPackageDataSource
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
    private readonly string _fileName;
    private IList<JsonWinGetPackageCatalog> _jsonCatalogs = new List<JsonWinGetPackageCatalog>();

    public override int CatalogCount => _jsonCatalogs.Count;

    public WinGetPackageJsonDataSource(
        ILogger logger,
        ISetupFlowStringResource stringResource,
        IWindowsPackageManager wpm,
        string fileName)
        : base(wpm)
    {
        _logger = logger;
        _stringResource = stringResource;
        _fileName = fileName;
    }

    public async override Task InitializeAsync()
    {
        // Open and deserialize JSON file
        using var fileStream = File.OpenRead(_fileName);
        var options = new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip };
        _jsonCatalogs = await JsonSerializer.DeserializeAsync<IList<JsonWinGetPackageCatalog>>(fileStream, options);
    }

    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        var result = new List<PackageCatalog>();
        foreach (var jsonCatalog in _jsonCatalogs)
        {
            var packageCatalog = await LoadCatalogAsync(jsonCatalog);
            if (packageCatalog != null)
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
            var packages = await GetPackagesAsync(jsonCatalog.WinGetPackageIds, id => id);
            if (packages.Any())
            {
                return new PackageCatalog()
                {
                    Name = _stringResource.GetLocalized(jsonCatalog.NameResourceKey),
                    Description = _stringResource.GetLocalized(jsonCatalog.DescriptionResourceKey),
                    Packages = packages.ToReadOnlyCollection(),
                };
            }
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetPackageJsonDataSource), LogLevel.Info, $"Error loading packages from winget catalog: {e.Message}");
        }

        return null;
    }
}
