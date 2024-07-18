// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.TaskGroups;
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
    private readonly SetupFlowOrchestrator _orchestrator;

    public ConfigurationFileBuilder(SetupFlowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Builds an object that represents a config file that can be used by WinGet Configure to install
    /// apps and clone repositories.This is already formatted as valid yaml and can be written
    /// directly to a file.
    /// </summary>
    /// <returns>The config file object representing the yaml file.</returns>
    public WinGetConfigFile BuildConfigFileObjectFromTaskGroups(IList<ISetupTaskGroup> taskGroups, ConfigurationFileKind configurationFileKind)
    {
        var listOfResources = new List<WinGetConfigResource>();

        foreach (var taskGroup in taskGroups)
        {
            if (taskGroup is RepoConfigTaskGroup repoConfigGroup)
            {
                // Add the GitDSC resource blocks to yaml
                listOfResources.AddRange(GetResourcesForCloneTaskGroup(repoConfigGroup, configurationFileKind));
            }
            else if (taskGroup is AppManagementTaskGroup appManagementGroup)
            {
                // Add the WinGetDsc resource blocks to yaml
                listOfResources.AddRange(GetResourcesForAppManagementTaskGroup(appManagementGroup, configurationFileKind));
            }
        }

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

    /// <summary>
    /// Creates a list of WinGetConfigResource objects from the CloneRepoTask objects in the RepoConfigTaskGroup
    /// </summary>
    /// <param name="repoConfigGroup">Clone repository task group where cloning information is located</param>
    /// <returns>List of objects that represent a WinGet configure resource block</returns>
    private List<WinGetConfigResource> GetResourcesForCloneTaskGroup(RepoConfigTaskGroup repoConfigGroup, ConfigurationFileKind configurationFileKind)
    {
        var listOfResources = new List<WinGetConfigResource>();
        var repoConfigTasks = repoConfigGroup.SetupTasks
            .Where(task => task is CloneRepoTask)
            .Select(task => task as CloneRepoTask)
            .ToList();

        if (repoConfigTasks.Count != 0)
        {
            listOfResources.Add(CreateWinGetInstallForGitPreReq());
        }

        foreach (var repoConfigTask in repoConfigTasks)
        {
            if (repoConfigTask.RepositoryToClone is GenericRepository genericRepository)
            {
                listOfResources.Add(CreateResourceFromTaskForGitDsc(repoConfigTask, genericRepository.RepoUri, configurationFileKind));
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
        var installList = appManagementGroup.SetupTasks
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

        if (configurationFileKind == ConfigurationFileKind.SetupTarget)
        {
            // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
            // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
            id = $"{arguments.PackageId} | Install: " + task.PackageName;
        }

        return new WinGetConfigResource()
        {
            Resource = DscHelpers.WinGetDscResource,
            Id = id,
            Directives = new() { AllowPrerelease = true, Description = $"Installing {arguments.PackageId}" },
            Settings = new WinGetDscSettings() { Id = arguments.PackageId, Source = DscHelpers.DscSourceNameForWinGet },
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
        // For normal cases, the Id will be null. This can be changed in the future when a use case for this Dsc File builder is needed outside the setup
        // setup target flow. We can likely drop the if statement and just use whats in its body.
        string id = null;
        var gitDependsOnId = DscHelpers.GitWinGetPackageId;

        if (configurationFileKind == ConfigurationFileKind.SetupTarget)
        {
            // WinGet configure uses the Id property to uniquely identify a resource and also to display the resource status in the UI.
            // So we add a description to the Id to make it more readable in the UI. These do not need to be localized.
            id = $"Clone {task.RepositoryName}" + ": " + task.CloneLocation.FullName;
            gitDependsOnId = $"{DscHelpers.GitWinGetPackageId} | Install: {DscHelpers.GitName}";
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
    private WinGetConfigResource CreateWinGetInstallForGitPreReq()
    {
        return new WinGetConfigResource()
        {
            Resource = DscHelpers.WinGetDscResource,
            Id = $"{DscHelpers.GitWinGetPackageId} | Install: {DscHelpers.GitName}",
            Directives = new() { AllowPrerelease = true, Description = $"Installing {DscHelpers.GitName}" },
            Settings = new WinGetDscSettings() { Id = DscHelpers.GitWinGetPackageId, Source = DscHelpers.DscSourceNameForWinGet },
        };
    }
}
