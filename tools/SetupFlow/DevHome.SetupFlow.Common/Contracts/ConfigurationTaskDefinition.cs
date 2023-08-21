// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;

namespace DevHome.SetupFlow.Common.Contracts;

public sealed class ConfigurationTaskDefinition : TaskDefinition
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

    public static ConfigurationTaskDefinition ReadCliArgument(string[] args, ref int index)
    {
        const int length = 4;
        if (index + length <= args.Length &&
            args[index] == _configFile &&
            args[index + 2] == _configContent)
        {
            var result = new ConfigurationTaskDefinition
            {
                FilePath = args[index + 1],
                Content = args[index + 3],
            };
            index += length;
            return result;
        }

        return null;
    }

    public override string ToCliArgument()
    {
        return $"{_configFile} \"{FilePath}\" {_configContent} \"{Content}\"";
    }
}
