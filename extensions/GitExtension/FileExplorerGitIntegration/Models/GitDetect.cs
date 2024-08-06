// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;
using Microsoft.Windows.DevHome.SDK;
using Serilog;

namespace FileExplorerGitIntegration.Models;

public class GitDetect
{
    public GitConfiguration GitConfiguration { get; set; }

    private readonly ILogger _log = Log.ForContext<GitDetect>();

    public GitDetect()
    {
        GitConfiguration = new GitConfiguration(null);
    }

    public bool DetectGit()
    {
        var gitExeFound = false;

        if (!gitExeFound)
        {
            // Check if git.exe is present in PATH environment variable
            gitExeFound = ValidateGitConfigurationPath("git.exe");
            if (gitExeFound)
            {
                GitConfiguration.StoreGitExeInstallPath("git.exe");
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
                    gitExeFound = CheckForExeInPaths(paths);
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
            gitExeFound = CheckForExeInPaths(possiblePaths);
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
                _log.Warning("Install location does not exist: {InstallLocation}", installLocation);
                return Array.Empty<string>();
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to find subdirectories in install location: {InstallLocation}", installLocation);
            return Array.Empty<string>();
        }
    }

    private bool CheckForExeInPaths(string[] possiblePaths)
    {
        // Iterate through the possible paths to find the git.exe file
        foreach (var path in possiblePaths.Where(x => !string.IsNullOrEmpty(x)))
        {
            var gitPath = Path.Combine(path, "git.exe");
            var isValid = ValidateGitConfigurationPath(gitPath);

            // If the git.exe file is found, store the install path and log the information
            if (isValid)
            {
                GitConfiguration.StoreGitExeInstallPath(gitPath);
                _log.Information("Git Exe Install Path found");
                return true;
            }
        }

        _log.Debug("Git.exe not found in paths examined");
        return false;
    }

    public bool ValidateGitConfigurationPath(string path)
    {
        var result = GitExecute.ExecuteGitCommand(path, string.Empty, "--version");
        if (result.Status == ProviderOperationStatus.Success && result.Output != null && result.Output.Contains("git version"))
        {
            return true;
        }

        return false;
    }
}
