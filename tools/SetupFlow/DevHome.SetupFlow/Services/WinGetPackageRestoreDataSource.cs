// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Models;
using DevHome.Telemetry;
using Microsoft.Diagnostics.Telemetry.Internal;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;
using Serilog;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Services;

public class WinGetPackageRestoreDataSource : WinGetPackageDataSource
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WinGetPackageRestoreDataSource));
    private readonly IRestoreInfo _restoreInfo;
    private readonly ISetupFlowStringResource _stringResource;
    private IRestoreDeviceInfo _restoreDeviceInfo;

    public WinGetPackageRestoreDataSource(
        ISetupFlowStringResource stringResource,
        IWindowsPackageManager wpm,
        IRestoreInfo restoreInfo)
        : base(wpm)
    {
        _stringResource = stringResource;
        _restoreInfo = restoreInfo;
    }

    /// <summary>
    /// Gets the total number of package catalogs available in this data source
    /// </summary>
    /// <remarks>
    /// Each collection of packages from a restore device is compiled into a
    /// catalog. At most show one catalog.
    /// </remarks>
    public override int CatalogCount => _restoreDeviceInfo == null ? 0 : 1;

    /// <summary>
    /// Gets the restore device information
    /// </summary>
    public async override Task InitializeAsync()
    {
        var restoreDeviceInfoResult = await _restoreInfo.GetRestoreDeviceInfoAsync();
        if (restoreDeviceInfoResult.Status == RestoreDeviceInfoStatus.Ok)
        {
            _restoreDeviceInfo = restoreDeviceInfoResult.RestoreDeviceInfo;
            TelemetryFactory.Get<ITelemetry>().Log("AppInstall_RestoreApps_Found", LogLevel.Critical, new EmptyEvent(PartA_PrivTags.ProductAndServiceUsage));
        }
        else
        {
            _log.Warning($"Restore data source skipped with status: {restoreDeviceInfoResult.Status}");
        }
    }

    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        var result = new List<PackageCatalog>();
        if (_restoreDeviceInfo == null)
        {
            _log.Warning($"Load catalogs skipped because no restore device information was found");
            return result;
        }

        try
        {
            _log.Information("Finding packages from restore data");
            var packages = await GetPackagesAsync(_restoreDeviceInfo.WinGetApplicationsInfo.Select(p => GetPackageUri(p)).ToList());
            foreach (var package in packages)
            {
                var packageUri = WindowsPackageManager.CreatePackageUri(package);
                _log.Information($"Obtaining icon information for restore package {package.Id}");
                var appInfo = _restoreDeviceInfo.WinGetApplicationsInfo.FirstOrDefault(p => packageUri == GetPackageUri(p));
                if (appInfo != null)
                {
                    package.LightThemeIcon = await GetRestoreApplicationIconAsync(appInfo, RestoreApplicationIconTheme.Light);
                    package.DarkThemeIcon = await GetRestoreApplicationIconAsync(appInfo, RestoreApplicationIconTheme.Dark);
                }
            }

            if (packages.Any())
            {
                result.Add(new PackageCatalog()
                {
                    Name = _stringResource.GetLocalized(StringResourceKey.RestorePackagesTitle, _restoreDeviceInfo.DisplayName),
                    Description = GetDescription(),
                    Packages = packages.ToReadOnlyCollection(),
                });
            }
            else
            {
                _log.Information("No packages found from restore");
            }
        }
        catch (Exception e)
        {
            _log.Error(e, $"Error loading packages from winget restore catalog.");
        }

        if (result.Count > 0)
        {
            TelemetryFactory.Get<ITelemetry>().Log("AppInstall_RestoreApps_Loaded", LogLevel.Critical, new EmptyEvent(PartA_PrivTags.ProductAndServiceUsage));
        }

        return result;
    }

    /// <summary>
    /// Get the icon for a restore application based on the provided theme
    /// </summary>
    /// <param name="appInfo">Restore application</param>
    /// <param name="theme">Target theme</param>
    /// <returns>Restore application icon stream, or null if no corresponding icon was found</returns>
    private async Task<IRandomAccessStream> GetRestoreApplicationIconAsync(IRestoreApplicationInfo appInfo, RestoreApplicationIconTheme theme)
    {
        try
        {
            // Load icon from restore app data
            var iconTask = appInfo.GetIconAsync(theme);

            // Check if no icon is available
            if (iconTask != null)
            {
                var icon = await iconTask;

                // Ensure stream is not empty to prevent rendering an empty image
                if (icon.Size > 0)
                {
                    return icon;
                }
            }
        }
        catch (Exception e)
        {
            _log.Error(e, $"Failed to get icon for restore package {appInfo.Id}");
        }

        _log.Warning($"No {theme} icon found for restore package {appInfo.Id}. A default one will be provided.");
        return null;
    }

    /// <summary>
    /// Gets the restore catalog description
    /// </summary>
    /// <returns>Localized restore catalog description</returns>
    private string GetDescription()
    {
        // Check if last modified time is not available
        if (_restoreDeviceInfo.LastModifiedTime == DateTime.MinValue)
        {
            return _stringResource.GetLocalized(StringResourceKey.RestorePackagesDescription, _restoreDeviceInfo.DisplayName);
        }

        return _stringResource.GetLocalized(StringResourceKey.RestorePackagesDescriptionWithDate, _restoreDeviceInfo.DisplayName, _restoreDeviceInfo.LastModifiedTime.ToString("d", CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Gets the package URI for a restore application
    /// </summary>
    /// <param name="appInfo">Application information</param>
    /// <returns>Package URI</returns>
    /// <remarks>All restored applications are from winget catalog</remarks>
    private WinGetPackageUri GetPackageUri(IRestoreApplicationInfo appInfo)
    {
        return WindowsPackageManager.CreateWinGetCatalogPackageUri(appInfo.Id);
    }
}
