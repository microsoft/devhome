// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using Serilog;
using Windows.Storage;

namespace FileExplorerGitIntegration.Models;

public class GitConfiguration : IDisposable
{
    public GitExecutableConfigOptions GitExecutableConfigOptions { get; set; }

    private readonly FileService _fileService;

    private string GitExeInstallPath { get; set; } = string.Empty;

    private readonly object _fileLock = new();

    private readonly ILogger _log = Log.ForContext<GitDetect>();

    private readonly string _tempConfigurationFileName = "TemporaryGitConfiguration.json";

    public GitConfiguration(string? path)
    {
        string folderPath;
        if (RuntimeHelper.IsMSIX)
        {
            folderPath = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            folderPath = path ?? string.Empty;
        }

        GitExecutableConfigOptions = new GitExecutableConfigOptions
        {
            GitExecutableConfigFolderPath = folderPath,
        };

        _fileService = new FileService();
        EnsureConfigFileCreation();
    }

    public string ReadInstallPath()
    {
        lock (_fileLock)
        {
            GitExeInstallPath = _fileService.Read(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName, GitConfigurationSourceGenerationContext.Default.String);
            return GitExeInstallPath;
        }
    }

    public void EnsureConfigFileCreation()
    {
        lock (_fileLock)
        {
            if (!Directory.Exists(GitExecutableConfigOptions.GitExecutableConfigFolderPath))
            {
                Directory.CreateDirectory(GitExecutableConfigOptions.GitExecutableConfigFolderPath);
            }

            var configFileFullPath = Path.Combine(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName);
            if (!File.Exists(configFileFullPath))
            {
                _fileService.Save(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName, string.Empty, GitConfigurationSourceGenerationContext.Default.String);
                _log.Information("The git configuration file did not exists and has just been created");
            }
        }
    }

    public bool IsGitExeInstallPathSet()
    {
        return !string.IsNullOrEmpty(GitExeInstallPath);
    }

    public bool StoreGitExeInstallPath(string path)
    {
        lock (_fileLock)
        {
            _log.Information("Setting Git Exe Install Path");
            GitExeInstallPath = path;

            _fileService.Save(GitExecutableConfigOptions.GitExecutableConfigFolderPath, _tempConfigurationFileName, GitExeInstallPath, GitConfigurationSourceGenerationContext.Default.String);
            File.Replace(Path.Combine(GitExecutableConfigOptions.GitExecutableConfigFolderPath, _tempConfigurationFileName), Path.Combine(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName), null);
            _log.Information("Git Exe Install Path stored successfully");
            return true;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

[JsonSerializable(typeof(string))]
internal sealed partial class GitConfigurationSourceGenerationContext : JsonSerializerContext
{
}
