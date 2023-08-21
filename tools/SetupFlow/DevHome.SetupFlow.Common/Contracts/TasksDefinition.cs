// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

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

    public static TasksDefinition FromCliArgument(string[] args, int index)
    {
        TasksDefinition tasksDefinition = new ()
        {
            Install = new List<InstallTaskDefinition>(),
        };

        static void TrimArgName(string[] args, int index)
        {
            if (index < args.Length)
            {
                args[index] = args[index].Replace(System.Environment.NewLine, string.Empty);
            }
        }

        while (index < args.Length)
        {
            TrimArgName(args, index);
            var install = InstallTaskDefinition.ReadCliArgument(args, ref index);

            TrimArgName(args, index);
            var devDrive = DevDriveTaskDefinition.ReadCliArgument(args, ref index);

            TrimArgName(args, index);
            var config = ConfigurationTaskDefinition.ReadCliArgument(args, ref index);

            if (install != null)
            {
                tasksDefinition.Install.Add(install);
            }

            if (config != null)
            {
                tasksDefinition.Configuration = config;
            }

            if (devDrive != null)
            {
                tasksDefinition.DevDrive = devDrive;
            }
        }

        return tasksDefinition;
    }

    public string ToCliArgument()
    {
        var newLine = System.Environment.NewLine;
        StringBuilder str = new ();
        if (Install != null)
        {
            foreach (var task in Install)
            {
                str.Append(newLine).Append(task.ToCliArgument());
            }
        }

        if (Configuration != null)
        {
            str.Append(newLine).Append(Configuration.ToCliArgument());
        }

        if (DevDrive != null)
        {
            str.Append(newLine).Append(DevDrive.ToCliArgument());
        }

        return str.ToString();
    }
}
