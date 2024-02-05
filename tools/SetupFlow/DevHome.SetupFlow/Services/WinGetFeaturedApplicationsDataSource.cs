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

/// <summary>
/// Class to get featured applications from extension providers.
/// </summary>
public class WinGetFeaturedApplicationsDataSource : WinGetPackageDataSource
{
    private readonly IExtensionService _extensionService;
    private int _catalogCount;

    public WinGetFeaturedApplicationsDataSource(IWindowsPackageManager wpm, IExtensionService extensionService)
        : base(wpm)
    {
        _extensionService = extensionService;
    }

    /// <inheritdoc />
    public override int CatalogCount => _catalogCount;

    /// <inheritdoc />
    public async override Task InitializeAsync()
    {
        // During initialization, get the total number of groups from all extensions
        // The total number of groups can change at runtime if an extension
        // was enabled/disabled or installed/uninstalled
        _catalogCount = 0;
        await ForEachEnabledExtensionAsync(async (extensionGroups) =>
        {
            // Get the total number of packages from all groups
            _catalogCount += extensionGroups.Count;
            await Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        // Recompute the number of groups in case an extension was
        // enabled/disabled or installed/uninstalled
        _catalogCount = 0;
        var result = new List<PackageCatalog>();
        await ForEachEnabledExtensionAsync(async (extensionGroups) =>
        {
            // Update the catalog count in case the extension added or removed groups
            _catalogCount += extensionGroups.Count;
            foreach (var group in extensionGroups)
            {
                try
                {
                    var catalog = await LoadCatalogAsync(group);
                    if (catalog != null)
                    {
                        result.Add(catalog);
                    }
                }
                catch (Exception e)
                {
                    Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading packages from featured applications group.", e);
                }
            }
        });

        return result;
    }

    /// <summary>
    /// Load a catalog from a featured applications group.
    /// </summary>
    /// <param name="group">Featured applications group</param>
    /// <returns>Package catalog, or null if no packages found or an error occurred</returns>
    private async Task<PackageCatalog> LoadCatalogAsync(IFeaturedApplicationsGroup group)
    {
        var locale = CultureInfo.CurrentCulture.Name;
        var groupTitle = group.GetTitle(locale);
        var appsResult = group.GetApplications();
        if (appsResult.Result.Status == ProviderOperationStatus.Success)
        {
            var packages = await GetPackagesAsync(ParseURIs(appsResult.FeaturedApplications));
            if (packages.Any())
            {
                return new ()
                {
                    Name = groupTitle,
                    Description = group.GetDescription(locale),
                    Packages = packages.ToReadOnlyCollection(),
                };
            }
            else
            {
                Log.Logger?.ReportInfo(Log.Component.AppManagement, $"No packages found in featured applications group '{groupTitle}'");
            }
        }
        else
        {
            Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Failed to get featured applications group '{groupTitle}': {appsResult.Result.DiagnosticText}", appsResult.Result.ExtendedError);
        }

        return null;
    }

    /// <summary>
    /// Parse packages URIs from a list of strings.
    /// </summary>
    /// <param name="uriStrings">List of package URI strings</param>
    /// <returns>List of package URIs</returns>
    private List<Uri> ParseURIs(IReadOnlyList<string> uriStrings)
    {
        var result = new List<Uri>();
        foreach (var app in uriStrings)
        {
            if (Uri.TryCreate(app, UriKind.Absolute, out var uri))
            {
                result.Add(uri);
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.AppManagement, $"Invalid package uri: {app}");
            }
        }

        return result;
    }

    /// <summary>
    /// Execute an action for each featured applications group from all enabled extensions.
    /// </summary>
    /// <param name="action">Action to execute</param>
    private async Task ForEachEnabledExtensionAsync(Func<IReadOnlyList<IFeaturedApplicationsGroup>, Task> action)
    {
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Getting featured applications from all extensions");
        var extensions = await _extensionService.GetInstalledExtensionsAsync(ProviderType.FeaturedApplications);
        foreach (var extension in extensions)
        {
            var extensionName = extension.Name;

            try
            {
                if (_extensionService.IsEnabled(extension.ExtensionUniqueId))
                {
                    Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Getting featured applications provider from extension '{extensionName}'");
                    var provider = await extension.GetProviderAsync<IFeaturedApplicationsProvider>();
                    if (provider != null)
                    {
                        var groupsResult = await provider.GetFeaturedApplicationsGroupsAsync();
                        if (groupsResult.Result.Status == ProviderOperationStatus.Success)
                        {
                            var groups = groupsResult.FeaturedApplicationsGroups;

                            // Copy list items to the current process
                            // Cannot use foreach or LINQ for out-of-process IVector
                            // Bug: https://github.com/microsoft/CsWinRT/issues/1205
                            var groupList = new List<IFeaturedApplicationsGroup>();
                            for (var i = 0; i < groups.Count; ++i)
                            {
                                groupList.Add(groups[i]);
                            }

                            Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Found {groups.Count} groups from extension '{extensionName}'");
                            await action(groupList);
                        }
                        else
                        {
                            Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to get featured applications groups from extension '{extensionName}': {groupsResult.Result.DiagnosticText}", groupsResult.Result.ExtendedError);
                        }
                    }
                    else
                    {
                        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Skipping featured applications groups from extension '{extensionName}' because it's not enabled");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading featured applications from extension {extensionName}", e);
            }
        }
    }
}
