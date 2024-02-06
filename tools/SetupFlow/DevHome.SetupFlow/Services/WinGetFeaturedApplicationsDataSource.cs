// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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
public sealed class WinGetFeaturedApplicationsDataSource : WinGetPackageDataSource, IDisposable
{
    private readonly IExtensionService _extensionService;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool disposedValue;

    /// <summary>
    /// Gets the estimated total count of catalogs from all enabled extensions.
    /// </summary>
    /// <remarks>
    /// For <see cref="WinGetFeaturedApplicationsDataSource"/> this is an
    /// estimate because the count may change at runtime
    /// </remarks>
    private int _estimatedCatalogCount;

    public WinGetFeaturedApplicationsDataSource(IWindowsPackageManager wpm, IExtensionService extensionService)
        : base(wpm)
    {
        _extensionService = extensionService;
    }

    /// <inheritdoc />
    public override int CatalogCount => _estimatedCatalogCount;

    /// <inheritdoc />
    public async override Task InitializeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // During initialization, get the estimated total count of catalogs
            _estimatedCatalogCount = 0;
            await ForEachEnabledExtensionAsync(async (extensionGroups) =>
            {
                _estimatedCatalogCount += extensionGroups.Count;
                await Task.CompletedTask;
            });
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            // Recompute the estimated total count of catalogs when loading
            // catalogs from each extension in case the extension added/removed
            // catalogs, was enabled/disabled or was installed/uninstalled
            _estimatedCatalogCount = 0;

            var result = new List<PackageCatalog>();
            await ForEachEnabledExtensionAsync(async (extensionGroups) =>
            {
                _estimatedCatalogCount += extensionGroups.Count;
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
        finally
        {
            _lock.Release();
        }
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
                return new()
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
                    Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to get featured applications provider from extension '{extensionName}'");
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading featured applications from extension {extensionName}", e);
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _lock.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
