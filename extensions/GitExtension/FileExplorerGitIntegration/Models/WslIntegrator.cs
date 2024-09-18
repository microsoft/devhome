// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;

namespace FileExplorerGitIntegration.Models;

public class WslIntegrator
{
    private static readonly string[] _wslPathPrefixes = { "wsl$", "wsl.localhost" };

    // For more information on distributions, see https://github.com/microsoft/WSL/blob/master/distributions/DistributionInfo.json
    private static readonly string[] _wslDistributions =
    {
        "Ubuntu", "Debian", "kali-linux", "Ubuntu-18.04", "Ubuntu-20.04", "Ubuntu-22.04", "Ubuntu-24.04", "OracleLinux_7_9", "OracleLinux_8_7", "OracleLinux_9_1",
        "openSUSE-Leap-15.6", "SUSE-Linux-Enterprise-15-SP5", "SUSE-Linux-Enterprise-15-SP6", "openSUSE-Tumbleweed",
    };

    public static bool IsWSLRepo(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return false;
        }

        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // Check if the repository path contains any of the WSL path prefixes
        foreach (string prefix in _wslPathPrefixes)
        {
            if (pathParts[0].Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetWslDistributionName(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return string.Empty;
        }

        // Parse the repository path to get the distribution name
        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length >= 1)
        {
            foreach (string distribution in _wslDistributions)
            {
                if (pathParts[1].Equals(distribution, StringComparison.OrdinalIgnoreCase))
                {
                    return distribution;
                }
            }
        }

        return string.Empty;
    }

    public static string GetWorkingDirectoryPath(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return string.Empty;
        }

        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var workingDirPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts.Skip(2));
        workingDirPath = workingDirPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        workingDirPath = workingDirPath.Insert(0, Path.AltDirectorySeparatorChar.ToString());
        return workingDirPath;
    }

    public static string GetWorkingDirectory(string repositoryPath)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return string.Empty;
        }

        string[] pathParts = repositoryPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // Ensure the first part is replaced with "\\wsl$"
        if (pathParts.Length > 0)
        {
            pathParts[0] = Path.DirectorySeparatorChar + "\\wsl$";
        }

        var workingDirPath = string.Join(Path.DirectorySeparatorChar.ToString(), pathParts);

        return workingDirPath;
    }

    public static string GetArgumentPrefixForWsl(string repositoryPath)
    {
        if (!IsWSLRepo(repositoryPath))
        {
            return string.Empty;
        }

        var distributionName = GetWslDistributionName(repositoryPath);
        if (distributionName == string.Empty)
        {
            return string.Empty;
        }

        string argumentPrefix = $"-d {distributionName} git ";
        return argumentPrefix;
    }
}
