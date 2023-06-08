// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;
using Windows.Storage.Streams;

namespace DevHome.SetupFlow.Services;
public class WinGetPackageRestoreDataSource : WinGetPackageDataSource
{
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
        }
        else
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Restore data source skipped with status: {restoreDeviceInfoResult.Status}");
        }
    }

    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        var result = new List<PackageCatalog>();
        if (_restoreDeviceInfo == null)
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Load catalogs skipped because no restore device information was found");
            return result;
        }

        try
        {
            Log.Logger?.ReportInfo(Log.Component.AppManagement, "Finding packages from restore data");
            var packages = await GetPackagesAsync(
                _restoreDeviceInfo.WinGetApplicationsInfo,
                appInfo => appInfo.Id,
                async (package, appInfo) =>
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Obtaining icon information for restore package {package.Id}");
                package.LightThemeIcon = await GetRestoreApplicationIconAsync(appInfo, RestoreApplicationIconTheme.Light);
                package.DarkThemeIcon = await GetRestoreApplicationIconAsync(appInfo, RestoreApplicationIconTheme.Dark);
            });

            if (packages.Any())
            {
                result.Add(new PackageCatalog()
                {
                    Name = _stringResource.GetLocalized(StringResourceKey.RestorePackagesTitle, _restoreDeviceInfo.DisplayName),
                    Description = _stringResource.GetLocalized(StringResourceKey.RestorePackagesDescription, _restoreDeviceInfo.DisplayName),
                    Packages = packages.ToReadOnlyCollection(),
                });
            }
            else
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, "No packages found from restore");
            }
        }
        catch (Exception e)
        {
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading packages from winget restore catalog.", e);
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
            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to get icon for restore package {appInfo.Id}", e);
        }

        Log.Logger?.ReportWarn(Log.Component.AppManagement, $"No {theme} icon found for restore package {appInfo.Id}. A default one will be provided.");
        return null;
    }
}
