﻿// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.SetupFlow.Services;
public class WinGetFeaturedApplicationsDataSource : WinGetPackageDataSource
{
    private readonly IPluginService _pluginService;
    private readonly IList<IFeaturedApplicationsGroup> _groups;

    public WinGetFeaturedApplicationsDataSource(IWindowsPackageManager wpm, IPluginService pluginService)
        : base(wpm)
    {
        _pluginService = pluginService;
        _groups = new List<IFeaturedApplicationsGroup>();
    }

    public override int CatalogCount => _groups.Count;

    public async override Task InitializeAsync()
    {
        var plugins = await _pluginService.GetInstalledPluginsAsync(ProviderType.FeaturedApplications);
        foreach (var plugin in plugins)
        {
            var provider = await plugin.GetProviderAsync<IFeaturedApplicationsProvider>();
            if (provider != null)
            {
                var groupsResult = await provider.GetFeaturedApplicationsGroupsAsync();
                if (groupsResult.Result.Status == ProviderOperationStatus.Success)
                {
                    for (var i = 0; i < groupsResult.FeaturedApplicationsGroups.Count; ++i)
                    {
                        _groups.Add(groupsResult.FeaturedApplicationsGroups[i]);
                    }
                }
                else
                {
                    Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get featured application groups", groupsResult.Result.ExtendedError);
                }
            }
        }
    }

    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        var result = new List<PackageCatalog>();
        foreach (var group in _groups)
        {
            try
            {
                var appsResult = group.GetApplications();
                if (appsResult.Result.Status == ProviderOperationStatus.Success)
                {
                    var packages = await GetPackagesAsync(appsResult.FeaturedApplications.ToList(), id => id);
                    if (packages.Any())
                    {
                        var locale = CultureInfo.CurrentCulture.Name;
                        result.Add(new PackageCatalog()
                        {
                            Name = group.GetTitle(locale),
                            Description = group.GetDescription(locale),
                            Packages = packages.ToReadOnlyCollection(),
                        });
                    }
                    else
                    {
                        Log.Logger?.ReportInfo(Log.Component.AppManagement, "No packages found from feature applications");
                    }
                }
                else
                {
                    Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get featured applications", appsResult.Result.ExtendedError);
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading packages from winget restore catalog.", e);
            }
        }

        return result;
    }
}
