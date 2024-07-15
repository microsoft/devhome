// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCFile
{
    /// <summary>
    /// Gets the configuration file name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the configuration file directory path
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Gets the configuration file content
    /// </summary>
    public string Content { get; }
}
