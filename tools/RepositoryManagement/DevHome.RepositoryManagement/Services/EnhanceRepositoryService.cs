// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Services;
using DevHome.Common.TelemetryEvents.RepositoryManagement;
using DevHome.Common.Windows.FileDialog;
using DevHome.Customization.ViewModels;
using DevHome.Telemetry;
using FileExplorerSourceControlIntegration;
using Microsoft.UI.Xaml;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace DevHome.RepositoryManagement.Services;

public class EnhanceRepositoryService
{
    private const string ErrorEventName = "DevHome_EnhanceRepositoryError_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(EnhanceRepositoryService));

    private readonly FileExplorerViewModel _sourceControlRegistrar;

    private readonly Window _window;

    private readonly ITelemetry _telemetry;

    private readonly List<IExtensionWrapper> _repositorySourceControlProviders;

    public EnhanceRepositoryService(
        FileExplorerViewModel sourceControlRegistrar,
        Window window,
        IExtensionService extensionService)
    {
        _sourceControlRegistrar = sourceControlRegistrar;
        _telemetry = TelemetryFactory.Get<ITelemetry>();
        _window = window;
        _repositorySourceControlProviders = extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository).Result.ToList();
    }

    /// <summary>
    /// Gets the location of an existing repository and hook up the path to both
    /// the source control provider and file explorer.
    /// </summary>
    /// <returns>Path to the new repository.  String.Empty for any errors</returns>
    public async Task<(string RepositoryLocation, Guid sourceControlClassId)> SelectRepositoryAndMakeItEnhanced()
    {
        var maybeRepositoryPath = await GetRepositoryLocationFromUser();
        var sourceControlId = await MakeRepositoryEnhanced(maybeRepositoryPath);
        return (maybeRepositoryPath, sourceControlId);
    }

    public async Task<Guid> MakeRepositoryEnhanced(string repositoryLocation)
    {
        _sourceControlRegistrar.AddRepositoryAlreadyOnMachine(repositoryLocation);
        var sourceControlId = await AssignSourceControlToPath(repositoryLocation);
        return sourceControlId;
    }

    public string GetLocalBranchName(string repositoryLocation)
    {
        try
        {
            var repository = new LibGit2Sharp.Repository(repositoryLocation);
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
            var repository = new LibGit2Sharp.Repository(repositoryLocation);
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

    private async Task<string> GetRepositoryLocationFromUser()
    {
        StorageFolder repositoryRootFolder = null;
        try
        {
            // TODO: Test that this method of calling telemetry works.
            // Storing ITelemetry instead of calling TelemtryFactory.Get<ITelemetry>()
            using var folderDialog = new WindowOpenFolderDialog();
            repositoryRootFolder = await folderDialog.ShowAsync(_window);
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Error occurred when selecting a folder for adding a repository.");
            _telemetry.LogError(
                ErrorEventName,
                LogLevel.Critical,
                new EnhanceRepositoryErrorEvent(nameof(SelectRepositoryAndMakeItEnhanced), ex.HResult, ex.Message, string.Empty));
            return string.Empty;
        }

        if (repositoryRootFolder == null || repositoryRootFolder.Path.Length == 0)
        {
            _log.Information("Didn't select a location to register");
            return string.Empty;
        }

        _log.Information($"Selected '{repositoryRootFolder.Path}' as location to register");
        return repositoryRootFolder.Path;
    }

    private async Task<Guid> AssignSourceControlToPath(string maybeRepositoryPath)
    {
        if (string.IsNullOrEmpty(maybeRepositoryPath))
        {
            _log.Information("maybeRepositoryPath is null or empty.  Not assigning a source control provider");
            return Guid.Empty;
        }

        if (!Directory.Exists(maybeRepositoryPath))
        {
            Directory.CreateDirectory(maybeRepositoryPath);
        }

        foreach (var extension in _repositorySourceControlProviders)
        {
            var didAdd = await _sourceControlRegistrar.AssignSourceControlProviderToRepository(extension, maybeRepositoryPath);
            if (didAdd.Result == Customization.Helpers.ResultType.Success)
            {
                _log.Information($"Source control {extension.ExtensionDisplayName} is assigned to repository {maybeRepositoryPath}");
                if (Guid.TryParse(extension.ExtensionClassId, out Guid id))
                {
                    _log.Information($"Successfully assigned the extension class Id of {extension.ExtensionClassId}");
                    return id;
                }
            }
        }

        _log.Information($"Did not find any source extensions for repository {maybeRepositoryPath}");
        return Guid.Empty;
    }
}
