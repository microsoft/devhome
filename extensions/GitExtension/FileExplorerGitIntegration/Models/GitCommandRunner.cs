// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using DevHome.Common.Helpers;
using DevHome.Common.Services;
using FileExplorerGitIntegration.Helpers;
using Microsoft.Win32;
using Microsoft.Windows.DevHome.SDK;
using Serilog;
using Windows.Storage;

namespace FileExplorerGitIntegration.Models;

public class GitCommandRunner : IDisposable
{
    public GitExecutableConfigOptions GitExecutableConfigOptions { get; set; }

    private readonly FileService fileService;

    private string GitExeInstallPath { get; set; } = string.Empty;

    private readonly FileSystemWatcher fileWatcher;

    private readonly ILogger log = Log.ForContext<GitCommandRunner>();

    public GitCommandRunner(string? path)
    {
        if (RuntimeHelper.IsMSIX)
        {
            GitExecutableConfigOptions = new GitExecutableConfigOptions
            {
                GitExecutableConfigFolderPath = ApplicationData.Current.LocalFolder.Path,
            };
        }
        else
        {
            GitExecutableConfigOptions = new GitExecutableConfigOptions
            {
                GitExecutableConfigFolderPath = path ?? string.Empty,
            };
        }

        fileService = new FileService();
        ReadInstallPath();

        fileWatcher = new FileSystemWatcher(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName);
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        fileWatcher.Changed += OnFileChanged;
        fileWatcher.EnableRaisingEvents = true;
        log.Debug("FileSystemWatcher initialized for configuration file");
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (args.Name == GitExecutableConfigOptions.GitExecutableConfigFileName)
        {
            ReadInstallPath();
        }
    }

    public void ReadInstallPath()
    {
        GitExeInstallPath = fileService.Read<string>(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName);
    }

    public bool IsGitExeInstallPathSet()
    {
        return !string.IsNullOrEmpty(GitExeInstallPath);
    }

    public GitCommandRunnerResultInfo ValidateGitExeInstallPath()
    {
        try
        {
            System.Diagnostics.Process.Start(GitExeInstallPath);
            return new GitCommandRunnerResultInfo(ProviderOperationStatus.Success, null);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to start Git.exe at configured path");
            return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, "Failed to start Git.exe at configured path", string.Empty, ex, null);
        }
    }

    public bool StoreGitExeInstallPath(string path)
    {
        log.Information("Setting Git Exe Install Path");
        GitExeInstallPath = path;
        fileService.Save(GitExecutableConfigOptions.GitExecutableConfigFolderPath, GitExecutableConfigOptions.GitExecutableConfigFileName, GitExeInstallPath);
        log.Information("Git Exe Install Path stored successfully");
        return true;
    }

    public bool DetectGit()
    {
        var gitExeFound = false;

        if (!gitExeFound)
        {
            // Check if git.exe is present in PATH environment variable
            try
            {
                System.Diagnostics.Process.Start("git.exe");
                gitExeFound = true;
                StoreGitExeInstallPath("git.exe");
            }
            catch (Exception ex)
            {
                log.Debug(ex, "Failed to start Git.exe");
            }
        }

        if (!gitExeFound)
        {
            // Check execution of git.exe by finding install location in registry keys
            string[] registryPaths = { "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Git_is1", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Git_is1" };

            foreach (var registryPath in registryPaths)
            {
                var gitPath = Registry.GetValue(registryPath, "InstallLocation", defaultValue: string.Empty) as string;
                if (!string.IsNullOrEmpty(gitPath))
                {
                    var paths = FindSubdirectories(gitPath);
                    CheckForExeInPaths(paths, ref gitExeFound);
                    if (gitExeFound)
                    {
                        break;
                    }
                }
            }
        }

        if (!gitExeFound)
        {
            // Search for git.exe in common file paths
            var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            string[] possiblePaths = { $"{programFiles}\\Git\\bin", $"{programFilesX86}\\Git\\bin", $"{programFiles}\\Git\\cmd", $"{programFilesX86}\\Git\\cmd" };
            CheckForExeInPaths(possiblePaths, ref gitExeFound);
        }

        return gitExeFound;
    }

    private string[] FindSubdirectories(string installLocation)
    {
        try
        {
            if (Directory.Exists(installLocation))
            {
                return Directory.GetDirectories(installLocation);
            }
            else
            {
                log.Warning("Install location does not exist: {InstallLocation}", installLocation);
                return Array.Empty<string>();
            }
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Failed to find subdirectories in install location: {InstallLocation}", installLocation);
            return Array.Empty<string>();
        }
    }

    private void CheckForExeInPaths(string[] possiblePaths, ref bool gitExeFound)
    {
        foreach (var path in possiblePaths)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var gitPath = Path.Combine(path, "git.exe");
                    Process.Start(gitPath);
                    StoreGitExeInstallPath(gitPath);
                    gitExeFound = true;
                    log.Information("Git Exe Install Path found");
                    break;
                }
                catch (Exception ex)
                {
                    log.Debug(ex, "Failed to start Git.exe while checking for executable in possible paths");
                }
            }
        }
    }

    public GitCommandRunnerResultInfo InvokeGitWithArguments(string? repositoryDirectory, string argument)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = GitExeInstallPath,
                Arguments = argument,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = repositoryDirectory ?? string.Empty,
            };

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return new GitCommandRunnerResultInfo(ProviderOperationStatus.Success, output);
            }
            else
            {
                log.Error("Failed to start the Git process: process is null");
                return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, "Git process is null", string.Empty, new InvalidOperationException("Failed to start the Git process: process is null"), null);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Failed to invoke Git with arguments: {Argument}", argument);
            return new GitCommandRunnerResultInfo(ProviderOperationStatus.Failure, "Failed to invoke Git with arguments", string.Empty, ex, argument);
        }
    }

    public void Dispose()
    {
        fileWatcher.Dispose();
        GC.SuppressFinalize(this);
    }
}
