// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.SetupFlow.Common.Helpers;
using DevHome.SetupFlow.Models;
using DevHome.SetupFlow.Models.WingetConfigure;
using DevHome.SetupFlow.TaskGroups;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevHome.SetupFlow.Services;
public class ConfigurationFileBuilder
{
    private readonly SetupFlowOrchestrator _orchestrator;

    public ConfigurationFileBuilder(SetupFlowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Builds the yaml string that is used by WinGet Configure to install the apps and clone the repositories.
    /// This is already formatted as valid yaml and can be written directly to a file.
    /// </summary>
    /// <returns>The string representing the yaml file. This string is formatted as yaml.</returns>
    public string BuildConfigStringFromTaskGroups()
    {
        var appManagementGroup = _orchestrator.GetTaskGroup<AppManagementTaskGroup>();
        var repoConfigGroup = _orchestrator.GetTaskGroup<RepoConfigTaskGroup>();
        var wingetConfigProperties = new WinGetConfigProperties();
        var listOfResources = new List<WinGetConfigResource>();

        // Add the GitDSC resource blocks to yaml
        listOfResources.AddRange(GetResourcesForCloneTaskGroup(repoConfigGroup));

        // Add the WinGetDsc resource blocks to yaml
        listOfResources.AddRange(GetResourcesForAppManagementTaskGroup(appManagementGroup));

        // Remove duplicate resources with the same Id but keep ordering.  This is needed because the
        // Id of the resource should be unique as per winget configure requirements.
        listOfResources = listOfResources
            .GroupBy(resource => resource.Id)
            .Select(group => group.First())
            .ToList();

        // Merge the resources into the Resources property in the properties object
        wingetConfigProperties.Resources = listOfResources.ToArray();
        wingetConfigProperties.ConfigurationVersion = DscHelpers.WinGetConfigureVersion;

        // Create the new WinGetConfigFile object and serialize it to yaml
        var wingetConfigFile = new WinGetConfigFile() { Properties = wingetConfigProperties };
        var yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

        // Add the header banner and add two new lines after the header.
        var configStringWithHeader = DscHelpers.DevHomeHeaderBanner + Environment.NewLine + Environment.NewLine;
        var yaml = yamlSerializer.Serialize(wingetConfigFile);
        configStringWithHeader += yaml;
        return configStringWithHeader;
    }

    /// <summary>
    /// Creates a list of WinGetConfigResource objects from the CloneRepoTask objects in the RepoConfigTaskGroup
    /// </summary>
    /// <param name="repoConfigGroup">Clone repository task group where cloning information is located</param>
    /// <returns>List of objects that represent a WinGet configure resource block</returns>
    private List<WinGetConfigResource> GetResourcesForCloneTaskGroup(RepoConfigTaskGroup repoConfigGroup)
    {
        var listOfResources = new List<WinGetConfigResource>();
        var repoConfigTasks = repoConfigGroup.SetupTasks
            .Where(task => task is CloneRepoTask)
            .Select(task => task as CloneRepoTask)
            .ToList();

        if (repoConfigTasks.Any())
        {
            listOfResources.Add(CreateWinGetInstallForGitPreReq());
        }

        foreach (var repoConfigTask in repoConfigTasks)
        {
            if (repoConfigTask.RepositoryToClone is GenericRepository genericRepository)
            {
                listOfResources.Add(CreateResourceFromTaskForGitDsc(repoConfigTask, genericRepository.RepoUri));
            }
        }

        return listOfResources;
    }

    /// <summary>
    /// Creates a list of WinGetConfigResource objects from the InstallPackageTask objects in the AppManagementTaskGroup
    /// </summary>
    /// <param name="appManagementGroup">The task group that holds information about the apps the user wants to install</param>
    /// <returns>List of objects that represent a WinGet configure resource block</returns>
    private List<WinGetConfigResource> GetResourcesForAppManagementTaskGroup(AppManagementTaskGroup appManagementGroup)
    {
        var listOfResources = new List<WinGetConfigResource>();
        var installList = appManagementGroup.SetupTasks
            .Where(task => task is InstallPackageTask)
            .Select(task => task as InstallPackageTask)
            .ToList();

        foreach (var installTask in installList)
        {
            listOfResources.Add(CreateResourceFromTaskForWinGetDsc(installTask));
        }

        return listOfResources;
    }

    /// <summary>
    /// Creates a WinGetConfigResource object from an InstallPackageTask object.
    /// </summary>
    /// <param name="task">The install task with the package information for the app</param>
    /// <returns>The WinGetConfigResource object that represents the block of yaml needed by WinGetDsc to install the app. </returns>
    private WinGetConfigResource CreateResourceFromTaskForWinGetDsc(InstallPackageTask task)
    {
        var arguments = task.GetArguments();
        return new WinGetConfigResource()
        {
            Resource = DscHelpers.WinGetDscResource,
            Id = arguments.PackageId,
            Directives = new () { AllowPrerelease = true, Description = $"Installing {arguments.PackageId}" },
            Settings = new WinGetDscSettings() { Id = arguments.PackageId, Source = DscHelpers.DscSourceNameForWinGet },
        };
    }

    /// <summary>
    /// Creates a WinGetConfigResource object from a CloneRepoTask object.
    /// </summary>
    /// <param name="task">The task that includes the cloning information for the repository</param>
    /// <param name="webAddress">The url to the public Git repository</param>
    /// <returns>The WinGetConfigResource object that represents the block of yaml needed by GitDsc to clone the repository. </returns>
    private WinGetConfigResource CreateResourceFromTaskForGitDsc(CloneRepoTask task, Uri webAddress)
    {
        return new WinGetConfigResource()
        {
            Resource = DscHelpers.GitCloneDscResource,
            Id = webAddress.AbsolutePath,
            Directives = new () { AllowPrerelease = true, Description = $"Cloning: {task.RepositoryName}" },
            DependsOn = new[] { DscHelpers.GitDscWinGetId },
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
            Id = DscHelpers.GitDscWinGetId,
            Directives = new () { AllowPrerelease = true, Description = $"Installing {DscHelpers.GitName}" },
            Settings = new WinGetDscSettings() { Id = DscHelpers.GitDscWinGetId, Source = DscHelpers.DscSourceNameForWinGet },
        };
    }
}
