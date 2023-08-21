// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;

namespace DevHome.SetupFlow.Common.Contracts;

/// <summary>
/// Class representing a configuration task arguments
/// </summary>
public sealed class ConfigurationTaskArguments
{
    private const string _configFile = "--config-file";
    private const string _configContent = "--config-content";

    /// <summary>
    /// Gets or sets the configuration file path
    /// </summary>
    public string FilePath
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the configuration file content
    /// </summary>
    public string Content
    {
        get; set;
    }

    /// <summary>
    /// Try to read and parse argument list into an object.
    /// </summary>
    /// <param name="argumentList">Argument list</param>
    /// <param name="index">Index to start reading arguments from</param>
    /// <param name="result">Output object</param>
    /// <returns>True if reading arguments succeeded. False otherwise.</returns>
    public static bool TryReadArguments(IList<string> argumentList, ref int index, out ConfigurationTaskArguments result)
    {
        result = null;

        // --config-file <file>      --config-content <content>
        // [index]       [index + 1] [index + 2]      [index + 3]
        const int taskArgListCount = 4;
        if (index + taskArgListCount <= argumentList.Count &&
            argumentList[index] == _configFile &&
            argumentList[index + 2] == _configContent)
        {
            result = new ConfigurationTaskArguments
            {
                FilePath = argumentList[index + 1],
                Content = argumentList[index + 3],
            };
            index += taskArgListCount;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create a list of arguments from this object.
    /// </summary>
    /// <returns>List of argument strings from this object</returns>
    public List<string> ToArgumentList()
    {
        return new ()
        {
            _configFile, FilePath,        // --config-file <file>
            _configContent, Content,      // --config-content <content>
        };
    }
}
