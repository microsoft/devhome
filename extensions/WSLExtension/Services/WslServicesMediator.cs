// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;
using Windows.ApplicationModel;
using WSLExtension.ClassExtensions;
using WSLExtension.Contracts;
using WSLExtension.Exceptions;
using WSLExtension.Helpers;
using WSLExtension.Models;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;

namespace WSLExtension.Services;

/// <summary>
/// Class to interact with WSL services either through the wsl.exe process
/// or the registry.
/// </summary>
public class WslServicesMediator : IWslServicesMediator
{
    private const int FirstIndex = 0;

    private readonly PackageHelper _packageHelper = new();

    private readonly IProcessCreator _processCreator;

    private readonly string _distributionPackageExesLocation;

    public WslServicesMediator(IProcessCreator creator)
    {
        _processCreator = creator;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _distributionPackageExesLocation = Path.Combine(localAppData, "Microsoft", "WindowsApps");
    }

    /// <inheritdoc cref="IWslServicesMediator.GetAllNamesOfRunningDistributions"/>
    public HashSet<string> GetAllNamesOfRunningDistributions()
    {
        var processData = _processCreator.CreateProcessWithoutWindowAndWaitForExit(WslExe, ListAllRunningDistributions);

        // wsl.exe returns an error code when there are no distributions running. But in that case
        // it will send at least one line to standard output e.g "There are no running distributions."
        if (!processData.ExitedSuccessfully() && string.IsNullOrEmpty(processData.StdOutput))
        {
            throw new WslServicesMediatorException($"Unable to get all running distribution data" +
                $" process data: {processData}");
        }

        var distributions = new HashSet<string>();
        using var reader = new StringReader(processData.StdOutput);

        // Results for executing wsl.exe --list --running.
        // When no distributions are running the first line is localized: "There are no running distributions."
        // So we can skip it.
        // When there are distributions the first line is: "Windows Subsystem for Linux Distributions"
        // The rest of the lines are the running distribution names e.g
        // Debian (Default)
        // OracleLinux_7_9
        //
        // Note: the distribution that's set up as the default, will contain (default) next to it. But for our purposes
        // we don't need to read that part. We only need to first word of the space separated line. Distribution
        // names cannot have spaces so we don't need to worry about that either.
        reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            var spaceSeparatedArr = line.Split(" ");
            distributions.Add(spaceSeparatedArr[FirstIndex]);
        }

        return distributions;
    }

    /// <inheritdoc cref="IWslServicesMediator.IsDistributionRunning"/>
    public bool IsDistributionRunning(string distributionName)
    {
        return GetAllNamesOfRunningDistributions().Contains(distributionName);
    }

    /// <inheritdoc cref="IWslServicesMediator.GetAllRegisteredDistributions"/>
    /// <remarks>
    /// Method enumerates through the WSL registry location subkey location
    /// to retrieve information about each registered distribution.
    /// </remarks>
    public List<WslRegisteredDistribution> GetAllRegisteredDistributions()
    {
        var distributions = new List<WslRegisteredDistribution>();
        var linuxSubSystemKey = CurrentUser.OpenSubKey(WslRegistryLocation, false);

        if (linuxSubSystemKey == null)
        {
            return new();
        }

        var defaultDistribution = linuxSubSystemKey.GetValue(DefaultDistributionRegistryName) as string;

        foreach (var subKeyName in linuxSubSystemKey.GetSubKeyNames())
        {
            var subKey = linuxSubSystemKey.OpenSubKey(subKeyName);

            if (subKey == null)
            {
                continue;
            }

            var distribution = BuildDistributionInfoFromRegistry(subKey);
            if (string.IsNullOrEmpty(distribution.Name))
            {
                // distribution doesn't have a name. This would happen only if the users registry info
                // was messed up. WSL would likely not function properly either in these cases.
                continue;
            }

            // the last part of the registry subkey is the registered guid of the wsl distribution.
            var distributionGuid = subKey.Name.Split('\\').LastOrDefault() ?? string.Empty;
            if (!string.IsNullOrEmpty(defaultDistribution) &&
                defaultDistribution.Equals(distributionGuid, StringComparison.OrdinalIgnoreCase))
            {
                distribution.IsDefaultDistribution = true;
            }

            distributions.Add(distribution);
        }

        return distributions;
    }

    private WslRegisteredDistribution BuildDistributionInfoFromRegistry(RegistryKey registryKey)
    {
        var regDistributionName = registryKey.GetValue(DistributionRegistryName) as string ?? string.Empty;

        // The registry key name will be in the form of e.g:
        // HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Lxss\{387ef692-f68f-4f83-a1f1-30c891257bb6}
        // We need the last segment {387ef692-f68f-4f83-a1f1-30c891257bb6}. Each guid subkey under Lxss is a
        // registered WSL distribution and the WSL service uses these subkeys to store information about the
        // distribution.
        var lastSegmentOfRegistryKeyName = registryKey.Name.Split('\\').LastOrDefault();
        var version = registryKey.GetValue(WslVersion) as int?;
        var packageFamilyName = registryKey.GetValue(PackageFamilyRegistryName) as string;
        return new WslRegisteredDistribution(regDistributionName, lastSegmentOfRegistryKeyName, packageFamilyName, version);
    }

    /// <inheritdoc cref="IWslServicesMediator.UnregisterDistribution"/>
    public void UnregisterDistribution(string distributionName)
    {
        var processData = _processCreator.CreateProcessWithoutWindowAndWaitForExit(WslExe, UnregisterDistributionArgs.FormatArgs(distributionName));

        if (!processData.ExitedSuccessfully())
        {
            throw new WslServicesMediatorException($"Unable to launch distribution {distributionName} : {processData}");
        }
    }

    /// <inheritdoc cref="IWslServicesMediator.LaunchDistribution"/>
    public void LaunchDistribution(string distributionName, string? windowsTerminalProfile)
    {
        var executable = GetFileNameForProcessLaunch();

        // Only launch with terminal if its installed
        if (executable.Equals(WindowsTerminalShimExe, StringComparison.OrdinalIgnoreCase))
        {
            LaunchDistributionUsingTerminal(distributionName, windowsTerminalProfile);
            return;
        }

        // Default to starting the wsl process directly and passing in its command line args
        _processCreator.CreateProcessWithWindow(executable, LaunchDistributionWithoutTerminal.FormatArgs(distributionName));
    }

    private void LaunchDistributionUsingTerminal(string distributionName, string? windowsTerminalProfile)
    {
        var terminalArgs = LaunchDistributionInTerminalWithNoProfile.FormatArgs(distributionName);

        if (!string.IsNullOrEmpty(windowsTerminalProfile))
        {
            // Launch into terminal with the specified profile and run wsl.exe in the console window
            terminalArgs = LaunchDistributionInTerminalWithProfile.FormatArgs(windowsTerminalProfile, distributionName);
            _processCreator.CreateProcessWithWindow(WindowsTerminalShimExe, terminalArgs);
        }
        else
        {
            // Launch into terminal and run wsl.exe in the console window without a profile
            _processCreator.CreateProcessWithWindow(WindowsTerminalShimExe, terminalArgs);
        }
    }

    /// <inheritdoc cref="IWslServicesMediator.TerminateDistribution"/>
    public void TerminateDistribution(string distributionName)
    {
        var processData = _processCreator.CreateProcessWithoutWindowAndWaitForExit(WslExe, TerminateDistributionArgs.FormatArgs(distributionName));

        if (!processData.ExitedSuccessfully())
        {
            throw new WslServicesMediatorException($"Unable to terminate distribution {distributionName} : {processData}");
        }
    }

    /// <inheritdoc cref="IWslServicesMediator.InstallAndRegisterDistributionWithWithWslService"/>
    /// <remarks>
    /// Registers a wsl distribution by getting the details of its distribution launcher. Note: All WSL distributions found in the
    /// Microsoft Store must be created via the WSL distribution launcher project found here:
    /// https://github.com/microsoft/WSL-DistroLauncher. The WSL launcher allows WSL packages in the store to be registered with the WSL
    /// service via the "<launcher>.exe install" command. Where <launcher> is the name of the distributions application exe.
    /// These are always stored in %localappdata%\Microsoft\WindowsApps\<package-family-name> and only the applications
    /// executable file is placed in this location.
    /// </remarks>
    public void InstallAndRegisterDistributionWithWithWslService(Package distributionPackage)
    {
        var exeFileName = GetDistributionExecutableFromRegistry(distributionPackage.InstalledPath);
        var exeLocalAppPath = $@"{_distributionPackageExesLocation}\{distributionPackage.Id.FamilyName}\{exeFileName}";
        var processData = _processCreator.CreateProcessWithoutWindowAndWaitForExit(exeLocalAppPath, InstallAndRegisterDistributionArgs);

        if (!processData.ExitedSuccessfully())
        {
            throw new WslServicesMediatorException($"Unable to register {exeLocalAppPath} with WSL service : {processData}");
        }
    }

    private string GetFileNameForProcessLaunch()
    {
        return _packageHelper.IsPackageInstalled(WindowsTerminalPackageFamilyName) ? WindowsTerminalShimExe : WslExe;
    }

    /// <summary>
    /// Gets the executable name by using the app path registry location for the user.
    /// </summary>
    /// <param name="installationPath">Absolute path to the distribution package's installation location</param>
    /// <returns>The executable name including the .exe part of the name</returns>
    private string GetDistributionExecutableFromRegistry(string installationPath)
    {
        var appPathSubKey = CurrentUser.OpenSubKey(AppPathsRegistryLocation, false);

        if (appPathSubKey == null)
        {
            throw new WslServicesMediatorException($"Unable to access subkey {AppPathsRegistryLocation} to find {installationPath}");
        }

        foreach (var subKeyName in appPathSubKey.GetSubKeyNames())
        {
            var subKey = appPathSubKey.OpenSubKey(subKeyName);

            if (subKey == null)
            {
                continue;
            }

            // The subkey for the distribution contains a path value that points to the installation location of the distribution.
            var appPath = subKey.GetValue("path") as string ?? string.Empty;

            if (!string.IsNullOrEmpty(appPath) && appPath[appPath.Length - 1] == '\\')
            {
                // remove the ending backslash.
                appPath = appPath.Substring(0, appPath.Length - 1);
            }

            if (!appPath.Equals(installationPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (subKey.GetValue(null) is string absolutePathToExe)
            {
                // value will be in the form of:
                // C:\Program Files\WindowsApps\46932SUSE.openSUSETumbleweed_24177.7.109.0_x64__022rs5jcyhyac\openSUSE-Tumbleweed.exe
                // We only need the last part e.g openSUSE-Tumbleweed.exe.
                return absolutePathToExe.Split('\\').Last();
            }
        }

        throw new AppExecutionAliasNotFoundException($"App execution alias not found. No (Default) entry for {installationPath} in {AppPathsRegistryLocation}");
    }
}
