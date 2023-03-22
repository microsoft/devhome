// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.SetupFlow.Common.Services;
using DevHome.Telemetry;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;

namespace DevHome.SetupFlow.AppManagement.Services;
public class WinGetPackageRestoreDataSource : WinGetPackageDataSource
{
    private readonly IRestoreInfo _restoreInfo;
    private readonly ILogger _logger;
    private readonly ISetupFlowStringResource _stringResource;
    private IRestoreDeviceInfo _restoreDeviceInfo;

    public WinGetPackageRestoreDataSource(
        ILogger logger,
        ISetupFlowStringResource stringResource,
        IWindowsPackageManager wpm,
        IRestoreInfo restoreInfo)
        : base(wpm)
    {
        _logger = logger;
        _stringResource = stringResource;
        _restoreInfo = restoreInfo;
    }

    public override int CatalogCount => _restoreDeviceInfo == null ? 0 : 1;

    public async Task GetRestoreDeviceInfoAsync()
    {
        var restoreDeviceInfoResult = await _restoreInfo.GetRestoreDeviceInfoAsync();
        if (restoreDeviceInfoResult.Status == RestoreDeviceInfoStatus.Ok)
        {
            _restoreDeviceInfo = restoreDeviceInfoResult.RestoreDeviceInfo;
        }
        else
        {
            _logger.Log(nameof(WinGetPackageRestoreDataSource), LogLevel.Local, $"Restore data source skipped with status: {restoreDeviceInfoResult.Status}");
        }
    }

    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        var result = new List<PackageCatalog>();
        if (_restoreDeviceInfo == null)
        {
            _logger.Log(nameof(WinGetPackageRestoreDataSource), LogLevel.Local, $"Load catalogs skipped because no restore device information was found");
            return result;
        }

        try
        {
            var orderedPackages = await GetOrderedPackagesAsync(
                _restoreDeviceInfo.WinGetApplicationsInfo,
                appInfo => appInfo.Id,
                async (package, appInfo) =>
            {
                package.LightThemeIcon = await appInfo.GetIconAsync(RestoreApplicationIconTheme.Light);
                package.DarkThemeIcon = await appInfo.GetIconAsync(RestoreApplicationIconTheme.Dark);
            });

            if (orderedPackages.Any())
            {
                result.Add(new PackageCatalog()
                {
                    Name = _stringResource.GetLocalized(StringResourceKey.RestoreTitle, _restoreDeviceInfo.DisplayName),
                    Description = _stringResource.GetLocalized(StringResourceKey.RestoreDescription, _restoreDeviceInfo.DisplayName),
                    Packages = orderedPackages.ToReadOnlyCollection(),
                });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetPackageRestoreDataSource), LogLevel.Info, $"Error loading packages from winget catalog: {e.Message}");
        }

        return result;
    }
}
