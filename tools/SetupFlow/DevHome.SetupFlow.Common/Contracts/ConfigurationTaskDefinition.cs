// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;

public sealed class ConfigurationTaskDefinition : ITaskDefinition
{
    private const string _configFile = "--config-file";
    private const string _configContent = "--config-content";

    public string FilePath
    {
        get; set;
    }

    public string Content
    {
        get; set;
    }

    public static bool TryReadArguments(IList<string> tasksDefinitionArgumentList, ref int index, out ConfigurationTaskDefinition result)
    {
        result = null;
        const int taskArgListCount = 4;
        if (index + taskArgListCount <= tasksDefinitionArgumentList.Count &&
            tasksDefinitionArgumentList[index] == _configFile &&
            tasksDefinitionArgumentList[index + 2] == _configContent)
        {
            result = new ConfigurationTaskDefinition
            {
                FilePath = tasksDefinitionArgumentList[index + 1],
                Content = tasksDefinitionArgumentList[index + 3],
            };
            index += taskArgListCount;
            return true;
        }

        return false;
    }

    public List<string> ToArgumentList()
    {
        return new ()
        {
            _configFile, FilePath,
            _configContent, Content,
        };
    }
}
