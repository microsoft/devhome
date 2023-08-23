// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.SetupFlow.Common.Helpers;

namespace DevHome.SetupFlow.Common.Contracts;

/// <summary>
/// Class representing the set of command line arguments passed to the elevated
/// process. Includes arguments for all the tasks supported for elevation.
/// </summary>
public class TasksArguments
{
    /// <summary>
    /// Gets or sets the list of install package tasks arguments
    /// </summary>
    public List<InstallPackageTaskArguments> InstallPackages
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the dev drive task arguments
    /// </summary>
    public DevDriveTaskArguments DevDrive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the configuration task arguments
    /// </summary>
    public ConfigurationTaskArguments Configuration
    {
        get; set;
    }

    /// <summary>
    /// Parse argument list into an <see cref="TasksArguments"/>.
    /// </summary>
    /// <param name="argumentList">Argument list</param>
    /// <returns>Tasks arguments object</returns>
    public static TasksArguments FromArgumentList(IList<string> argumentList)
    {
        TasksArguments tasksArguments = new ()
        {
            InstallPackages = new List<InstallPackageTaskArguments>(),
        };

        var index = 0;
        while (index < argumentList.Count)
        {
            if (InstallPackageTaskArguments.TryReadArguments(argumentList, ref index, out var installPackageTaskArguments))
            {
                tasksArguments.InstallPackages.Add(installPackageTaskArguments);
            }
            else if (DevDriveTaskArguments.TryReadArguments(argumentList, ref index, out var devDriveTaskArguments))
            {
                tasksArguments.DevDrive = devDriveTaskArguments;
            }
            else if (ConfigurationTaskArguments.TryReadArguments(argumentList, ref index, out var configurationTaskArguments))
            {
                tasksArguments.Configuration = configurationTaskArguments;
            }
            else
            {
                Log.Logger?.ReportWarn(Log.Component.Elevated, $"Failed to parse input arguments: {string.Join(", ", argumentList.Skip(index))}");
                break;
            }
        }

        return tasksArguments;
    }

    /// <summary>
    /// Create a list of arguments from all tasks arguments.
    /// </summary>
    /// <returns>List of argument strings from all tasks arguments</returns>
    public List<string> ToArgumentList()
    {
        List<string> result = new ();
        if (InstallPackages != null)
        {
            result.AddRange(InstallPackages.SelectMany(def => def.ToArgumentList()));
        }

        if (Configuration != null)
        {
            result.AddRange(Configuration.ToArgumentList());
        }

        if (DevDrive != null)
        {
            result.AddRange(DevDrive.ToArgumentList());
        }

        return result;
    }
}
