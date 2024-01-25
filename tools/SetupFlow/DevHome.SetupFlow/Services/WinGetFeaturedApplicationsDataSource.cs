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
    private readonly List<IFeaturedApplicationsGroup> _groups;

    public WinGetFeaturedApplicationsDataSource(IWindowsPackageManager wpm, IExtensionService extensionService)
        : base(wpm)
    {
        _extensionService = extensionService;
        _groups = new List<IFeaturedApplicationsGroup>();
    }

    public override int CatalogCount => _groups.Count;

    public async override Task InitializeAsync()
    {
        var extensions = await _extensionService.GetInstalledExtensionsAsync(ProviderType.FeaturedApplications);
        Log.Logger?.ReportInfo(Log.Component.AppManagement, "Initializing featured applications from all extensions");
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
                        Log.Logger?.ReportInfo(Log.Component.AppManagement, $"Found {groups.Count} groups from extension '{extensionName}'");

                        // Cannot use foreach or LINQ for out-of-process IVector
                        // Bug: https://github.com/microsoft/CsWinRT/issues/1205
                        for (var i = 0; i < groups.Count; ++i)
                        {
                            _groups.Add(groups[i]);
                        }
                    }
                    else
                    {
                        Log.Logger?.ReportError(Log.Component.AppManagement, $"Failed to get featured applications groups from extension '{extensionName}': {groupsResult.Result.DiagnosticText}", groupsResult.Result.ExtendedError);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading featured applications from extension {extensionName}", e);
            }
        }
    }

    public async override Task<IList<PackageCatalog>> LoadCatalogsAsync()
    {
        var result = new List<PackageCatalog>();
        var locale = CultureInfo.CurrentCulture.Name;
        foreach (var group in _groups)
        {
            try
            {
                var groupTitle = group.GetTitle(locale);
                var appsResult = group.GetApplications();
                if (appsResult.Result.Status == ProviderOperationStatus.Success)
                {
                    var packages = await GetPackagesAsync(ParseURIs(appsResult.FeaturedApplications), uri => uri);
                    if (packages.Any())
                    {
                        result.Add(new PackageCatalog()
                        {
                            Name = groupTitle,
                            Description = group.GetDescription(locale),
                            Packages = packages.ToReadOnlyCollection(),
                        });
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
            }
            catch (Exception e)
            {
                Log.Logger?.ReportError(Log.Component.AppManagement, $"Error loading packages from featured applications group.", e);
            }
        }

        return result;
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
}
