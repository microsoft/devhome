// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Models;
using Serilog;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Services;

/// <summary>
/// Class for loading package catalogs from a JSON data source
/// </summary>
public class WinGetPackageJsonDataSource : WinGetPackageDataSource
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WinGetPackageJsonDataSource));

    /// <summary>
    /// Class for deserializing a JSON winget package
    /// </summary>
    private sealed class JsonWinGetPackage
    {
        public Uri Uri { get; set; }

        public string Icon { get; set; }

        public WinGetPackageUri GetPackageUri()
        {
            if (WinGetPackageUri.TryCreate(Uri, out var packageUri))
            {
                return packageUri;
            }

            return null;
        }
    }

    /// <summary>
    /// Class for deserializing a JSON package catalog with package ids from
    /// winget
    /// </summary>
    private sealed class JsonWinGetPackageCatalog
    {
        public string NameResourceKey { get; set; }

        public string DescriptionResourceKey { get; set; }

        public IList<JsonWinGetPackage> WinGetPackages { get; set; }
    }

    private readonly ISetupFlowStringResource _stringResource;
    private readonly string _fileName;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip };
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
        _log.Information($"Reading package list from JSON file {_fileName}");
        using var fileStream = File.OpenRead(_fileName);

        _jsonCatalogs = await JsonSerializer.DeserializeAsync<IList<JsonWinGetPackageCatalog>>(fileStream, jsonSerializerOptions);
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

    private List<WinGetPackageUri> GetPackageUris(IList<JsonWinGetPackage> jsonPackages)
    {
        var result = new List<WinGetPackageUri>();
        foreach (var jsonPackage in jsonPackages)
        {
            var packageUri = jsonPackage.GetPackageUri();
            if (packageUri != null)
            {
                result.Add(packageUri);
            }
            else
            {
                _log.Warning($"Skipping {jsonPackage.Uri} because it is not a valid winget package uri");
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
        _log.Information($"Attempting to read JSON package catalog {catalogName}");

        try
        {
            var packageUris = GetPackageUris(jsonCatalog.WinGetPackages);
            var packages = await GetPackagesAsync(packageUris);
            _log.Information($"Obtaining icon information for JSON packages: [{string.Join(", ", packages.Select(p => $"({p.Name}, {p.CatalogName})"))}]");
            foreach (var package in packages)
            {
                var packageUri = WindowsPackageManager.CreatePackageUri(package);
                var jsonPackage = jsonCatalog.WinGetPackages.FirstOrDefault(p => packageUri.Equals(p.GetPackageUri(), WinGetPackageUriParameters.None));
                if (jsonPackage != null)
                {
                    var icon = await GetJsonApplicationIconAsync(jsonPackage);
                    package.LightThemeIcon = icon;
                    package.DarkThemeIcon = icon;
                }
            }

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
                _log.Warning($"JSON package catalog [{catalogName}] is empty");
            }
        }
        catch (Exception e)
        {
            _log.Error(e, $"Error loading packages from winget catalog.");
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
            _log.Error(e, $"Failed to get icon for JSON package {package.Uri}.");
        }

        _log.Warning($"No icon found for JSON package {package.Uri}. A default one will be provided.");
        return null;
    }
}
