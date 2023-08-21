// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using DevHome.SetupFlow.Common.Helpers;

namespace DevHome.SetupFlow.Common.Contracts;
public sealed class TasksDefinition
{
    public List<InstallTaskDefinition> Install
    {
        get; set;
    }

    public DevDriveTaskDefinition DevDrive
    {
        get; set;
    }

    public ConfigurationTaskDefinition Configuration
    {
        get; set;
    }

    public static TasksDefinition FromArgumentList(IList<string> tasksDefinitionArgumentList)
    {
        TasksDefinition tasksDefinition = new ()
        {
            Install = new List<InstallTaskDefinition>(),
        };

        var index = 0;
        while (index < tasksDefinitionArgumentList.Count)
        {
            if (InstallTaskDefinition.TryReadArguments(tasksDefinitionArgumentList, ref index, out var installTaskDefinition))
            {
                tasksDefinition.Install.Add(installTaskDefinition);
            }
            else if (DevDriveTaskDefinition.TryReadArguments(tasksDefinitionArgumentList, ref index, out var devDriveTaskDefinition))
            {
                tasksDefinition.DevDrive = devDriveTaskDefinition;
            }
            else if (ConfigurationTaskDefinition.TryReadArguments(tasksDefinitionArgumentList, ref index, out var configurationTaskDefinition))
            {
                tasksDefinition.Configuration = configurationTaskDefinition;
            }
            else
            {
                Log.Logger.ReportWarn(Log.Component.Elevated, $"Failed to parse input arguments: {string.Join(", ", tasksDefinitionArgumentList.Skip(index))}");
                break;
            }
        }

        return tasksDefinition;
    }

    public List<string> ToArgumentList()
    {
        List<string> result = new ();
        if (Install != null)
        {
            result.AddRange(Install.SelectMany(def => def.ToArgumentList()));
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
