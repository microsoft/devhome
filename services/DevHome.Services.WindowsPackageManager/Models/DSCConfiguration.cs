// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DevHome.Services.WindowsPackageManager.Models;

/// <summary>
/// Model class for a YAML configuration file
/// </summary>
public class DSCConfiguration
{
    private readonly ILogger _logger;
    private readonly FileInfo _fileInfo;
    private readonly Lazy<string> _lazyContent;

    public DSCConfiguration(ILogger logger, string filePath)
    {
        _logger = logger;
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
        _logger.LogInformation($"Loading configuration file content from {_fileInfo.FullName}");
        using var text = _fileInfo.OpenText();
        return text.ReadToEnd();
    }
}
