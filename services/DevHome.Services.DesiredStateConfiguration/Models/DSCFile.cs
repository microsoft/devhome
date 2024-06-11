// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.Services.DesiredStateConfiguration.Models;

/// <summary>
/// Model class for a YAML configuration file
/// </summary>
internal sealed class DSCFile : IDSCFile
{
    private readonly FileInfo _fileInfo;

    private DSCFile(string filePath, string content = null)
    {
        _fileInfo = new FileInfo(filePath);
        Content = content;
    }

    /// <summary>
    /// Load a configuration file from a path.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The configuration file.</returns>
    public static async Task<IDSCFile> LoadAsync(string filePath)
    {
        var file = new DSCFile(filePath);
        await file.LoadContentAsync();
        return file;
    }

    /// <summary>
    /// Create a virtual file with the specified content without writing to disk.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="content">Content of the file</param>
    /// <returns>The configuration file.</returns>
    public static IDSCFile CreateVirtual(string filePath, string content)
    {
        Debug.Assert(content != null, "Content must not be null");
        return new DSCFile(filePath, content);
    }

    /// <inheritdoc/>
    public string Name => _fileInfo.Name;

    /// <inheritdoc/>
    public string Path => _fileInfo.FullName;

    /// <inheritdoc/>
    public string DirectoryPath => _fileInfo.Directory.FullName;

    /// <inheritdoc/>
    public string Content { get; private set; }

    /// <summary>
    /// Load configuration file content
    /// </summary>
    private async Task LoadContentAsync()
    {
        using var text = _fileInfo.OpenText();
        Content = await text.ReadToEndAsync();
    }
}
