// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Class for loading package catalogs from a JSON data source
/// </summary>
public class WinGetPackageJsonDataSource : WinGetPackageDataSource
{
    /// <summary>
    /// Class for deserializing a JSON winget package
    /// </summary>
    private class JsonWinGetPackage
    {
        public string Id { get; set; }

        public string Icon { get; set; }
    }

    /// <summary>
    /// Class for deserializing a JSON package catalog with package ids from
    /// winget
    /// </summary>
    private class JsonWinGetPackageCatalog
    {
        public string NameResourceKey { get; set; }

        public string DescriptionResourceKey { get; set; }

        public IList<JsonWinGetPackage> WinGetPackages { get; set; }
    }

    private readonly ISetupFlowStringResource _stringResource;
    private readonly string _fileName;
    private IList<JsonWinGetPackageCatalog> _jsonCatalogs = new List<JsonWinGetPackageCatalog>();

    public override int CatalogCount => _jsonCatalogs.Count;

    public WinGetPackageJsonDataSource(
        ISetupFlowStringResource stringResource,
        IWindowsPackageManager wpm,
        string fileName)
        : base(wpm)
    {
        _stringResource = stringResource;
        _fileName = fileName;
    }

    public async override Task InitializeAsync()
    {
        // Open and deserialize JSON file
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Reading package list from JSON file {_fileName}");
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
        var catalogName = _stringResource.GetLocalized(jsonCatalog.NameResourceKey);
        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Attempting to read JSON package catalog {catalogName}");

        try
        {
            var packages = await GetPackagesAsync(
                jsonCatalog.WinGetPackages,
                package => package.Id,
                async (package, appInfo) =>
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Obtaining icon information for JSON package {package.Id}");
                var icon = await GetJsonApplicationIconAsync(appInfo);
                package.LightThemeIcon = icon;
                package.DarkThemeIcon = icon;
            });
            if (packages.Any())
            {
                return new PackageCatalog()
                {
                    Name = catalogName,
                    Description = _stringResource.GetLocalized(jsonCatalog.DescriptionResourceKey),
                    Packages = packages.ToReadOnlyCollection(),
                };
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"JSON package catalog [{catalogName}] is empty");
            }
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading packages from winget catalog.", e);
        }

        return null;
    }

    private async Task<IRandomAccessStream> GetJsonApplicationIconAsync(JsonWinGetPackage package)
    {
        try
        {
            if (!string.IsNullOrEmpty(package.Icon))
            {
                // Load icon from application assets
                var iconFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(package.Icon));
                var icon = await iconFile.OpenAsync(FileAccessMode.Read);

                // Ensure stream is not empty to prevent rendering an empty image
                if (icon.Size > 0)
                {
                    return icon;
                }
            }
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to get icon for JSON package {package.Id}.", e);
        }

        Log.Logger?.ReportWarn(Log.Component.AppManagement, $"No icon found for JSON package {package.Id}. A default one will be provided.");
        return null;
    }
}
