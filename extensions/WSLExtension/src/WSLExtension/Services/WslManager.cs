// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Win32;
using WSLExtension.Common;
using WSLExtension.DistroDefinitions;
using WSLExtension.Exceptions;
using WSLExtension.Extensions;
using WSLExtension.Helpers;
using WSLExtension.Models;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;
using static WSLExtension.DistroDefinitions.KnownDistributionHelper;
using static WSLExtension.Helpers.WslCommandOutputParser;

namespace WSLExtension.Services;

public class WslManager : IWslManager
{
    private readonly IProcessCreator _processCaller;

    private readonly IStringResource _stringResource;

    private readonly PackageHelper _packageHelper = new();

    private readonly object _lock = new();

    private Dictionary<string, KnownDistributionInfo>? _knownDistributionMap;

    public Dictionary<string, HashSet<Process>> DistributionRunningProcessMap { get; private set; } = new();

    private string? _base64StringForDefaultWslLogo;

    public WslManager(IProcessCreator processCaller, IStringResource stringResource)
    {
        _processCaller = processCaller;
        _stringResource = stringResource;
    }

    public async Task<List<WslRegisteredDistribution>> GetAllRegisteredDistributionsAsync()
    {
        _knownDistributionMap ??= await GetKnownDistributionInfoFromYamlAsync();
        var registeredDistributions = await GetRegisteredDistributionsAsync();
        var wslComputeSystems = new List<WslRegisteredDistribution>();

        foreach (var distribution in registeredDistributions)
        {
            wslComputeSystems.Add(new WslRegisteredDistribution(_stringResource, distribution.Value, this));
        }

        return wslComputeSystems;
    }

    public async Task<List<DistributionState>> GetKnownDistributionsFromMsStoreAsync()
    {
        var processData = _processCaller.CreateProcessWithoutWindow(WslExe, ListAllWslDistributionsFromMsStoreArgs);
        if (processData.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to retrieve all available WSL distributions from the Microsoft Store:" +
                $" StdOutput: {processData.StdError}");
        }

        var registeredDistributionsMap = await GetRegisteredDistributionsAsync();
        var distributionsToListOnCreationPage = new List<DistributionState>();
        _knownDistributionMap ??= await GetKnownDistributionInfoFromYamlAsync();
        _base64StringForDefaultWslLogo ??= await GetBase64StringFromLogoPathAsync(DefaultWslLogoPath);
        foreach (var distributionName in ParseKnownDistributionsFoundInMsStore(processData.StdOutput))
        {
            // filter out distributions that are already registered on machine.
            if (registeredDistributionsMap.TryGetValue(distributionName, out var _))
            {
                continue;
            }

            // Add known distributions that are common between our yaml file and the wsl --list --online output.
            if (_knownDistributionMap.TryGetValue(distributionName, out var knownDistributionInfo))
            {
                var distributionState = new DistributionState(knownDistributionInfo);

                // If an entry in the yaml file lacks a logo, we'll use the default wsl logo.
                if (string.IsNullOrEmpty(distributionState.Base64StringLogo))
                {
                    distributionState.Base64StringLogo = _base64StringForDefaultWslLogo;
                }

                distributionsToListOnCreationPage.Add(distributionState);
            }
            else
            {
                // This is a new distribution returned via the wsl --list --online command that we don't know about.
                distributionsToListOnCreationPage.Add(new(distributionName, _base64StringForDefaultWslLogo));
            }
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

    public async Task<DistributionState?> GetRegisteredDistributionAsync(string distributionName)
    {
        foreach (var registeredDistribution in (await GetRegisteredDistributionsAsync()).Values)
        {
            if (distributionName.Equals(registeredDistribution.DistributionName, StringComparison.Ordinal))
            {
                return registeredDistribution;
            }
        }

        return null;
    }

    private async Task<Dictionary<string, DistributionState>> GetRegisteredDistributionsAsync()
    {
        var distributions = new Dictionary<string, DistributionState>();

        var linuxSubSystemKey = CurrentUser.OpenSubKey(WslRegisryLocation, false);

        if (linuxSubSystemKey == null)
        {
            return distributions;
        }

        _knownDistributionMap ??= await GetKnownDistributionInfoFromYamlAsync();
        foreach (var subKeyName in linuxSubSystemKey.GetSubKeyNames())
        {
            var subKey = linuxSubSystemKey.OpenSubKey(subKeyName);

            if (subKey == null)
            {
                continue;
            }

            var distribution = BuildDistribution(subKey);

            // Add the logo and friendly name to the DistributionState object if its a distribution we
            // know about in KnownDistributionInfo.yaml.
            if (_knownDistributionMap.TryGetValue(distribution.DistributionName, out var knownDistributionInfo))
            {
                distribution.Base64StringLogo = knownDistributionInfo.Base64StringLogo;
                distribution.FriendlyName = knownDistributionInfo.FriendlyName;
            }

            distributions.Add(distribution.DistributionName, distribution);
        }

        return distributions;
    }

    private DistributionState BuildDistribution(RegistryKey registryKey)
    {
        // the distribution name should never be empty and is always a string.
        var regDistributionName = (string?)registryKey?.GetValue(DistributionName);
        var subkeyName = registryKey?.Name.Split('\\').Last();
        var isVersion2 = registryKey?.GetValue(WslVersion) as int? == 2;
        var packageFamilyName = registryKey?.GetValue(PackageFamilyName) as string;
        return new DistributionState(regDistributionName, subkeyName, packageFamilyName, isVersion2);
    }
}
