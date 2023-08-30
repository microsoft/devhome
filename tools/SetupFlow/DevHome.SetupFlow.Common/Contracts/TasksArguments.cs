// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
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
    /// Gets or sets the list of arguments for each of the install package tasks.
    /// </summary>
    public List<InstallPackageTaskArguments> InstallPackages
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the dev drive creation task arguments
    /// </summary>
    public CreateDevDriveTaskArguments CreateDevDrive
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the configuration task arguments
    /// </summary>
    public ConfigureTaskArguments Configure
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
                if (tasksArguments.InstallPackages.Any(p => p.PackageId == installPackageTaskArguments.PackageId && p.CatalogName == installPackageTaskArguments.CatalogName))
                {
                    throw new ArgumentException($"Duplicate install package task for package {installPackageTaskArguments.PackageId} in catalog {installPackageTaskArguments.CatalogName}");
                }

                tasksArguments.InstallPackages.Add(installPackageTaskArguments);
            }
            else if (CreateDevDriveTaskArguments.TryReadArguments(argumentList, ref index, out var devDriveTaskArguments))
            {
                if (tasksArguments.CreateDevDrive != null)
                {
                    throw new ArgumentException("Only one dev drive creation task can be specified");
                }

                tasksArguments.CreateDevDrive = devDriveTaskArguments;
            }
            else if (ConfigureTaskArguments.TryReadArguments(argumentList, ref index, out var configurationTaskArguments))
            {
                if (tasksArguments.Configure != null)
                {
                    throw new ArgumentException("Only one configuration task can be specified");
                }

                tasksArguments.Configure = configurationTaskArguments;
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

        if (Configure != null)
        {
            result.AddRange(Configure.ToArgumentList());
        }

        if (CreateDevDrive != null)
        {
            result.AddRange(CreateDevDrive.ToArgumentList());
        }

        return result;
    }
}
