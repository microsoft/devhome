// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Win32;
using WSLExtension.ClassExtensions;
using WSLExtension.Contracts;
using WSLExtension.DistributionDefinitions;
using WSLExtension.Exceptions;
using WSLExtension.Helpers;
using WSLExtension.Models;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;
using static WSLExtension.DistributionDefinitions.DistributionDefinitionHelper;
using static WSLExtension.Helpers.WslCommandOutputParser;

namespace WSLExtension.Services;

public class WslManager : IWslManager
{
    private readonly IProcessCreator _processCaller;

    private readonly IStringResource _stringResource;

    private readonly PackageHelper _packageHelper = new();

    private readonly object _lock = new();

    private string? _defaultWslLogoBase64String;

    private Dictionary<string, DistributionDefinition>? _distributionDefinitionsMap;

    public Dictionary<string, HashSet<Process>> DistributionRunningProcessMap { get; private set; } = new();

    public WslManager(IProcessCreator processCaller, IStringResource stringResource)
    {
        _processCaller = processCaller;
        _stringResource = stringResource;
    }

    public async Task<List<WslRegisteredDistribution>> GetAllRegisteredDistributionsAsync()
    {
        var wslComputeSystems = new List<WslRegisteredDistribution>();

        foreach (var distribution in await GetRegisteredDistributionsAsync())
        {
            wslComputeSystems.Add(new WslRegisteredDistribution(_stringResource, distribution.Value, this));
        }

        return wslComputeSystems;
    }

    public async Task<List<WslDistributionInfo>> GetAllDistributionInfoFromGitHubAsync()
    {
        var registeredDistributionsMap = await GetRegisteredDistributionsAsync();
        var distributionsToListOnCreationPage = new List<WslDistributionInfo>();
        _distributionDefinitionsMap ??= await GetDistributionDefinitionsAsync();
        foreach (var distributionDefinition in _distributionDefinitionsMap.Values)
        {
            // filter out distribution definitions already registered on machine.
            if (registeredDistributionsMap.TryGetValue(distributionDefinition.Name, out var _))
            {
                continue;
            }

            distributionsToListOnCreationPage.Add(new(distributionDefinition));
        }

        return distributionsToListOnCreationPage;
    }

    public void UnregisterDistribution(string distributionName)
    {
        var arguments = UnregisterDistributionArgs.FormatArgs(distributionName);
        var processData = _processCaller.CreateProcessWithoutWindow(WslExe, arguments);

        if (processData.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to unregister the distro {distributionName}: StdOutput: {processData.StdError}");
        }
    }

    public WslProcessData InstallDistribution(string distributionName, DataReceivedEventHandler stdOutputHandler, DataReceivedEventHandler stdErrorHandler)
    {
        var arguments = InstallDistributionArgs.FormatArgs(distributionName);
        return _processCaller.CreateProcessWithoutWindow(WslExe, arguments, stdOutputHandler, stdErrorHandler);
    }

    public void LaunchDistribution(string distributionName)
    {
        var arguments = LaunchDistributionArgs.FormatArgs(distributionName);
        var process = _processCaller.CreateProcessWithWindow(GetFileNameForProcessLaunch(), arguments);
        lock (_lock)
        {
            if (DistributionRunningProcessMap.TryGetValue(distributionName, out var processList))
            {
                processList.Add(process);
            }
            else
            {
                DistributionRunningProcessMap.Add(distributionName, new HashSet<Process> { process });
            }

            process.EnableRaisingEvents = true;
            process.Exited += OnProcessClosed;
        }
    }

    public string GetFileNameForProcessLaunch()
    {
        return _packageHelper.IsPackageInstalled(WindowsTerminalPackageFamilyName) ? WindowsTerminalExe : CommandPromptExe;
    }

    public void OnProcessClosed(object? sender, EventArgs e)
    {
        if (sender is not Process process)
        {
            return;
        }

        lock (_lock)
        {
            foreach (var processSet in DistributionRunningProcessMap.Values)
            {
                if (processSet.Remove(process))
                {
                    break;
                }
            }
        }
    }

    public int SessionsInUseForDistribution(string distributionName)
    {
        lock (_lock)
        {
            if (DistributionRunningProcessMap.TryGetValue(distributionName, out var processSet))
            {
                return processSet.Count;
            }

            return 0;
        }
    }

    public async Task<WslDistributionInfo?> GetRegisteredDistributionAsync(string distributionName)
    {
        foreach (var registeredDistribution in (await GetRegisteredDistributionsAsync()).Values)
        {
            if (distributionName.Equals(registeredDistribution.Name, StringComparison.Ordinal))
            {
                return registeredDistribution;
            }
        }

        return null;
    }

    private async Task<Dictionary<string, WslDistributionInfo>> GetRegisteredDistributionsAsync()
    {
        var distributions = new Dictionary<string, WslDistributionInfo>();
        var linuxSubSystemKey = CurrentUser.OpenSubKey(WslRegistryLocation, false);

        if (linuxSubSystemKey == null)
        {
            return distributions;
        }

        _distributionDefinitionsMap ??= await GetDistributionDefinitionsAsync();
        foreach (var subKeyName in linuxSubSystemKey.GetSubKeyNames())
        {
            var subKey = linuxSubSystemKey.OpenSubKey(subKeyName);

            if (subKey == null)
            {
                continue;
            }

            var distribution = BuildDistributionInfoFromRegistry(subKey);

            // If this is a distribution we know aboutadd its friendly name and logo.
            if (_distributionDefinitionsMap.TryGetValue(distribution.Name, out var knownDistributionInfo))
            {
                distribution.FriendlyName = knownDistributionInfo.FriendlyName;
                distribution.Base64StringLogo = knownDistributionInfo.Base64StringLogo;
            }
            else
            {
                // Found a distribution in the registry we have no definitions for, so we'll use the default wsl logo as its logo.
                _defaultWslLogoBase64String ??= await GetBase64StringFromLogoPathAsync(WslLogoPathFormat.FormatArgs(DefaultWslLogoPath));
                distribution.Base64StringLogo = _defaultWslLogoBase64String;
            }

            distributions.Add(distribution.Name, distribution);
        }

        return distributions;
    }

    private WslDistributionInfo BuildDistributionInfoFromRegistry(RegistryKey registryKey)
    {
        // the distribution name should never be empty and is always a string.
        var regDistributionName = registryKey.GetValue(DistributionName) as string;
        var subkeyName = registryKey.Name.Split('\\').LastOrDefault();
        var isVersion2 = registryKey.GetValue(WslVersion) as int? == WslVersion2;
        var packageFamilyName = registryKey.GetValue(PackageFamilyName) as string;
        return new WslDistributionInfo(regDistributionName, subkeyName, packageFamilyName, isVersion2);
    }
}
