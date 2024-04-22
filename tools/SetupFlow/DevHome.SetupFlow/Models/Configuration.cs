// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Serilog;

namespace DevHome.SetupFlow.Models;

/// <summary>
/// Model class for a YAML configuration file
/// </summary>
public class Configuration
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(Configuration));
    private readonly FileInfo _fileInfo;
    private readonly Lazy<string> _lazyContent;

    public Configuration(string filePath)
    {
        _fileInfo = new FileInfo(filePath);
        _lazyContent = new(LoadContent);
    }

    /// <summary>
    /// Gets the configuration file name
    /// </summary>
    public string Name => _fileInfo.Name;

    public string Path => _fileInfo.FullName;

    /// <summary>
    /// Gets the file content
    /// </summary>
    public string Content => _lazyContent.Value;

    /// <summary>
    /// Load configuration file content
    /// </summary>
    /// <returns>Configuration file content</returns>
    private string LoadContent()
    {
        _log.Information($"Loading configuration file content from {_fileInfo.FullName}");
        using var text = _fileInfo.OpenText();
        return text.ReadToEnd();
    }
}
