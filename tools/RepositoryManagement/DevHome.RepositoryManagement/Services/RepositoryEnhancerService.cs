// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.Customization.Helpers;
using DevHome.Customization.ViewModels;
using DevHome.Telemetry;
using FileExplorerSourceControlIntegration;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;

namespace DevHome.RepositoryManagement.Services;

/// <summary>
/// Service for associating a local repository path with a source control extension.
/// </summary>
public class RepositoryEnhancerService
{
    private const string ErrorEventName = "DevHome_EnhanceRepositoryError_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryEnhancerService));

    private readonly FileExplorerViewModel _sourceControlRegistrar;

    private readonly Window _window;

    private readonly ITelemetry _telemetry;

    private readonly IExtensionService _extensionService;

    public RepositoryEnhancerService(
        FileExplorerViewModel sourceControlRegistrar,
        Window window,
        IExtensionService extensionService)
    {
        _sourceControlRegistrar = sourceControlRegistrar;
        _telemetry = TelemetryFactory.Get<ITelemetry>();
        _window = window;
        _extensionService = extensionService;
    }

    public List<IExtensionWrapper> GetAllSourceControlProviders()
    {
        return _extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository).Result.ToList();
    }

    /// <summary>
    /// Associates a source control provider with a local repository.
    /// </summary>
    /// <param name="repositoryLocation">The full path to the repositories root.</param>
    /// <returns>True if the association is made.  False otherwise</returns>
    public async Task<bool> MakeRepositoryEnhanced(string repositoryLocation, IExtensionWrapper sourceControlId)
    {
        _sourceControlRegistrar.AddRepositoryAlreadyOnMachine(repositoryLocation);
        return await AssignSourceControlToPath(repositoryLocation, sourceControlId);
    }

    public string GetLocalBranchName(string repositoryLocation)
    {
        try
        {
            using var repository = new LibGit2Sharp.Repository(repositoryLocation);
            return repository.Head.FriendlyName;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error when getting branch name");
        }

        return string.Empty;
    }

    public string GetRepositoryUrl(string repositoryLocation)
    {
        try
        {
            using var repository = new LibGit2Sharp.Repository(repositoryLocation);
            return repository.Network.Remotes.First().Url;
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error when getting the repositoryUrl");
        }

        return string.Empty;
    }

    public IPropertySet GetProperties(string[] propertiesToReturn, string repositoryLocation)
    {
        var sourceControlProvider = new SourceControlProvider();
        var provider = sourceControlProvider.GetProvider(repositoryLocation);

        if (provider == null)
        {
            _log.Warning($"Path: {repositoryLocation} does not have an associated provider.");
            return new PropertySet();
        }

        // This call does check the settings for file explorer and souce control integration.
        return provider.GetProperties(propertiesToReturn, string.Empty);
    }

    public async Task<SourceControlValidationResult> ReAssignSourceControl(string repositoryPath, IExtensionWrapper extensionWrapper)
    {
        return await _sourceControlRegistrar.AssignSourceControlProviderToRepository(extensionWrapper, repositoryPath);
    }

    private async Task<bool> AssignSourceControlToPath(string maybeRepositoryPath, IExtensionWrapper extension)
    {
        Directory.CreateDirectory(maybeRepositoryPath);

        var assignSourceControlResult = await _sourceControlRegistrar.AssignSourceControlProviderToRepository(extension, maybeRepositoryPath);
        if (assignSourceControlResult.Result == Customization.Helpers.ResultType.Success)
        {
            _log.Information($"Source control {extension.ExtensionDisplayName} is assigned to repository {maybeRepositoryPath}");
            return true;
        }

        return false;
    }
}
