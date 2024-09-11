// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevHome.Database.DatabaseModels.RepositoryManagement;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.TaskGroups;
using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevHome.SetupFlow.Services;

public enum ConfigurationFileKind
{
    Normal,
    SetupTarget,
}

public class ConfigurationFileBuilder
{
    public const string PackageNameSeparator = " | ";
    public const string RepoNamePrefix = "Clone ";
    public const string RepoNameSuffix = ": ";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(ConfigurationFileBuilder));

    /// <summary>
    /// Builds an object that represents a config file that can be used by WinGet Configure to install
    /// apps and clone repositories.This is already formatted as valid yaml and can be written
    /// directly to a file.
    /// </summary>
    /// <returns>The config file object representing the yaml file.</returns>
    public WinGetConfigFile BuildConfigFileObjectFromTaskGroups(IList<ISetupTaskGroup> taskGroups, ConfigurationFileKind configurationFileKind)
    {
        List<WinGetConfigResource> repoResources = [];
        List<WinGetConfigResource> appResources = [];
        foreach (var taskGroup in taskGroups)
        {
            if (taskGroup is RepoConfigTaskGroup repoConfigGroup)
            {
                // Add the GitDSC resource blocks to yaml
                repoResources.AddRange(GetResourcesForCloneTaskGroup(repoConfigGroup, configurationFileKind));
            }
            else if (taskGroup is AppManagementTaskGroup appManagementGroup)
            {
                // Add the WinGetDsc resource blocks to yaml
                appResources.AddRange(GetResourcesForAppManagementTaskGroup(appManagementGroup, configurationFileKind));
            }
        }

        // If Git is not added to the apps to install and there are
        // repositories to clone, add Git as a pre-requisite
        var isGitAdded = appResources
            .Select(r => r.Settings as WinGetDscSettings)
            .Any(s => s.Id == DscHelpers.GitWinGetPackageId);
        if (!isGitAdded && repoResources.Count > 0)
        {
            appResources.Add(CreateWinGetInstallForGitPreReq(configurationFileKind));
        }

        List<WinGetConfigResource> listOfResources = [.. appResources, .. repoResources];

        if (listOfResources.Count == 0)
        {
            return new WinGetConfigFile();
        }

        var wingetConfigProperties = new WinGetConfigProperties();

        // Remove duplicate resources with the same Id but keep ordering. This is needed because the
        // Id of the resource should be unique as per winget configure requirements.
        listOfResources = listOfResources
            .GroupBy(resource => resource.Id)
            .Select(group => group.First())
            .ToList();

        // Merge the resources into the Resources property in the properties object
        wingetConfigProperties.Resources = listOfResources.ToArray();
        wingetConfigProperties.ConfigurationVersion = DscHelpers.WinGetConfigureVersion;

        // Create the new WinGetConfigFile object and serialize it to yaml
        return new WinGetConfigFile() { Properties = wingetConfigProperties };
    }

    /// <summary>
    /// Builds the yaml string that is used by WinGet Configure to install the apps and clone the repositories.
    /// This is already formatted as valid yaml and can be written directly to a file.
    /// </summary>
    /// <returns>The string representing the yaml file. This string is formatted as yaml.</returns>
    public string BuildConfigFileStringFromTaskGroups(IList<ISetupTaskGroup> taskGroups, ConfigurationFileKind configurationFileKind)
    {
        // Create the new WinGetConfigFile object and serialize it to yaml
        var wingetConfigFile = BuildConfigFileObjectFromTaskGroups(taskGroups, configurationFileKind);
        return SerializeWingetFileObjectToString(wingetConfigFile);
    }

    /// <summary>
    /// Builds the yaml string that is used by WinGet Configure to install the apps and clone the repositories.
    /// This is already formatted as valid yaml and can be written directly to a file.
    /// </summary>
    /// <returns>The string representing the yaml file. This string is formatted as yaml.</returns>
    public string SerializeWingetFileObjectToString(WinGetConfigFile configFile)
    {
        // Create the new WinGetConfigFile object and serialize it to yaml
        var yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        // Add the header banner and add two new lines after the header.
        var configStringWithHeader = DscHelpers.DevHomeHeaderBanner + Environment.NewLine + Environment.NewLine;
        var yaml = yamlSerializer.Serialize(configFile);
        configStringWithHeader += yaml;
        return configStringWithHeader;
    }

    public string MakeConfigurationFileForRepoAndGit(Repository repository)
    {
        // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
        // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
        var id = $"{RepoNamePrefix}{repository.RepositoryName}{RepoNameSuffix}{Path.GetFullPath(repository.RepositoryClonePath)}";
        var gitDependsOnId = DscHelpers.GitWinGetPackageId;

        List<WinGetConfigResource> resources = [];

        resources.Add(new WinGetConfigResource()
        {
            Resource = DscHelpers.GitCloneDscResource,
            Id = id,
            Directives = new() { AllowPrerelease = true, Description = $"Cloning: {repository.RepositoryName}" },
            DependsOn = [gitDependsOnId],
            Settings = new GitDscSettings() { HttpsUrl = string.Empty, RootDirectory = Path.GetFullPath(repository.RepositoryClonePath) },
        });

        resources.Add(CreateWinGetInstallForGitPreReq(ConfigurationFileKind.Normal));

        var wingetConfigProperties = new WinGetConfigProperties();

        // Merge the resources into the Resources property in the properties object
        wingetConfigProperties.Resources = resources.ToArray();
        wingetConfigProperties.ConfigurationVersion = DscHelpers.WinGetConfigureVersion;

        // Create the new WinGetConfigFile object and serialize it to yaml
        var myFile = new WinGetConfigFile() { Properties = wingetConfigProperties };

        return SerializeWingetFileObjectToString(myFile);
    }

    /// <summary>
    /// Creates a list of WinGetConfigResource objects from the CloneRepoTask objects in the RepoConfigTaskGroup
    /// </summary>
    /// <param name="repoConfigGroup">Clone repository task group where cloning information is located</param>
    /// <returns>List of objects that represent a WinGet configure resource block</returns>
    private List<WinGetConfigResource> GetResourcesForCloneTaskGroup(RepoConfigTaskGroup repoConfigGroup, ConfigurationFileKind configurationFileKind)
    {
        var listOfResources = new List<WinGetConfigResource>();
        var repoConfigTasks = repoConfigGroup.DSCTasks
            .Where(task => task is CloneRepoTask)
            .Select(task => task as CloneRepoTask)
            .ToList();

        foreach (var repoConfigTask in repoConfigTasks)
        {
            try
            {
                if (!repoConfigTask.RepositoryToClone.IsPrivate)
                {
                    listOfResources.Add(CreateResourceFromTaskForGitDsc(repoConfigTask, repoConfigTask.RepositoryToClone.RepoUri, configurationFileKind));
                }
            }
            catch (Exception e)
            {
                _log.Error(e, $"Error creating a repository resource entry");
            }
        }

        return listOfResources;
    }

    /// <summary>
    /// Creates a list of WinGetConfigResource objects from the InstallPackageTask objects in the AppManagementTaskGroup
    /// </summary>
    /// <param name="appManagementGroup">The task group that holds information about the apps the user wants to install</param>
    /// <returns>List of objects that represent a WinGet configure resource block</returns>
    private List<WinGetConfigResource> GetResourcesForAppManagementTaskGroup(AppManagementTaskGroup appManagementGroup, ConfigurationFileKind configurationFileKind)
    {
        var listOfResources = new List<WinGetConfigResource>();
        var installList = appManagementGroup.DSCTasks
            .Where(task => task is InstallPackageTask)
            .Select(task => task as InstallPackageTask)
            .ToList();

        foreach (var installTask in installList)
        {
            listOfResources.Add(CreateResourceFromTaskForWinGetDsc(installTask, configurationFileKind));
        }

        return listOfResources;
    }

    /// <summary>
    /// Creates a WinGetConfigResource object from an InstallPackageTask object.
    /// </summary>
    /// <param name="task">The install task with the package information for the app</param>
    /// <returns>The WinGetConfigResource object that represents the block of yaml needed by WinGetDsc to install the app. </returns>
    private WinGetConfigResource CreateResourceFromTaskForWinGetDsc(InstallPackageTask task, ConfigurationFileKind configurationFileKind)
    {
        var arguments = task.GetArguments();
        var id = arguments.PackageId;
        var securityContext = WingGetConfigDirectives.SecurityContextCurrent;

        if (configurationFileKind == ConfigurationFileKind.SetupTarget)
        {
            // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
            // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
            id = $"{arguments.PackageId}{PackageNameSeparator}{task.PackageName}";

            if (arguments.IsElevationRequired)
            {
                securityContext = WingGetConfigDirectives.SecurityContextElevated;
            }
        }

        return new WinGetConfigResource()
        {
            Resource = DscHelpers.WinGetDscResource,
            Id = id,
            Directives = new()
            {
                AllowPrerelease = true,
                Description = $"Installing {arguments.PackageId}",
                SecurityContext = securityContext,
            },
            Settings = new WinGetDscSettings()
            {
                Id = arguments.PackageId,
                Source = arguments.CatalogName,
            },
        };
    }

    /// <summary>
    /// Creates a WinGetConfigResource object from a CloneRepoTask object.
    /// </summary>
    /// <param name="task">The task that includes the cloning information for the repository</param>
    /// <param name="webAddress">The url to the public Git repository</param>
    /// <returns>The WinGetConfigResource object that represents the block of yaml needed by GitDsc to clone the repository. </returns>
    private WinGetConfigResource CreateResourceFromTaskForGitDsc(CloneRepoTask task, Uri webAddress, ConfigurationFileKind configurationFileKind)
    {
        // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
        // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
        var id = $"{RepoNamePrefix}{task.RepositoryName}{RepoNameSuffix}{task.CloneLocation.FullName}";

        var gitDependsOnId = DscHelpers.GitWinGetPackageId;

        if (configurationFileKind == ConfigurationFileKind.SetupTarget)
        {
            gitDependsOnId = $"{DscHelpers.GitWinGetPackageId}{PackageNameSeparator}{DscHelpers.GitName}";
        }

        return new WinGetConfigResource()
        {
            Resource = DscHelpers.GitCloneDscResource,
            Id = id,
            Directives = new() { AllowPrerelease = true, Description = $"Cloning: {task.RepositoryName}" },
            DependsOn = [gitDependsOnId],
            Settings = new GitDscSettings() { HttpsUrl = webAddress.AbsoluteUri, RootDirectory = task.CloneLocation.FullName },
        };
    }

    /// <summary>
    /// Creates a WinGetConfigResource object for the GitDsc resource that installs Git for Windows. This is a pre-requisite for
    /// the GitDsc resource that clones the repository.
    /// </summary>
    /// <returns>The WinGetConfigResource object that represents the block of yaml needed by WinGetDsc to install the Git app.</returns>
    private WinGetConfigResource CreateWinGetInstallForGitPreReq(ConfigurationFileKind configurationFileKind)
    {
        var id = DscHelpers.GitWinGetPackageId;

        if (configurationFileKind == ConfigurationFileKind.SetupTarget)
        {
            // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
            // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
            id = $"{DscHelpers.GitWinGetPackageId}{PackageNameSeparator}{DscHelpers.GitName}";
        }

        return new WinGetConfigResource()
        {
            Resource = DscHelpers.WinGetDscResource,
            Id = id,
            Directives = new() { AllowPrerelease = true, Description = $"Installing {DscHelpers.GitName}" },
            Settings = new WinGetDscSettings() { Id = DscHelpers.GitWinGetPackageId, Source = DscHelpers.DscSourceNameForWinGet },
        };
    }
}
