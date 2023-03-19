// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.AppManagement.Models;
using DevHome.Telemetry;
using Microsoft.Internal.Windows.DevHome.Helpers;
using Microsoft.Internal.Windows.DevHome.Helpers.Restore;

namespace DevHome.SetupFlow.AppManagement.Services;
public class WinGetPackageRestoreDataSource
{
    private readonly RestoreInfo _restoreInfo = new ();
    private readonly ILogger _logger;
    private readonly IStringResource _stringResource;
    private readonly IWindowsPackageManager _wpm;

    public WinGetPackageRestoreDataSource(ILogger logger, IStringResource stringResource, IWindowsPackageManager wpm)
    {
        _logger = logger;
        _stringResource = stringResource;
        _wpm = wpm;
    }

    public async Task<PackageCatalog> LoadCatalogAsync()
    {
        var restoreDeviceInfoResult = await _restoreInfo.GetRestoreDeviceInfoAsync();
        if (restoreDeviceInfoResult.Status != RestoreDeviceInfoStatus.Ok)
        {
            _logger.Log(nameof(WinGetPackageRestoreDataSource), LogLevel.Local, $"Restore data source skipped with status: {restoreDeviceInfoResult.Status}");
            return null;
        }

        var appsInfo = restoreDeviceInfoResult.RestoreDeviceInfo.WinGetApplicationsInfo;
        try
        {
            // Get packages from winget catalog
            var unorderedPackages = await _wpm.WinGetCatalog.GetPackagesAsync(appsInfo.Select(appInfo => appInfo.Id).ToHashSet());
            var unorderedPackagesMap = unorderedPackages.ToDictionary(p => p.Id, p => p);

            // Sort result based on the input and set images
            List<IWinGetPackage> result = new ();
            foreach (var appInfo in appsInfo)
            {
                var package = unorderedPackagesMap.GetValueOrDefault(appInfo.Id, null);
                if (package != null)
                {
                    package.LightThemeIcon = await appInfo.GetIconAsync(RestoreApplicationIconTheme.Light);
                    package.DarkThemeIcon = await appInfo.GetIconAsync(RestoreApplicationIconTheme.Dark);
                    result.Add(package);
                }
            }

            return new PackageCatalog()
            {
                Name = restoreDeviceInfoResult.RestoreDeviceInfo.DisplayName,
                Description = restoreDeviceInfoResult.RestoreDeviceInfo.DisplayName,
                Packages = result.ToReadOnlyCollection(),
            };
        }
        catch (Exception e)
        {
            _logger.LogError(nameof(WinGetPackageRestoreDataSource), LogLevel.Info, $"Error loading packages from winget catalog: {e.Message}");
            return null;
        }
    }
}
