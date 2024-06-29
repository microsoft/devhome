// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Win32;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Win32;
using WSLExtension.Common;
using WSLExtension.DistroDefinitions;
using WSLExtension.Exceptions;
using WSLExtension.Extensions;
using WSLExtension.Helpers;
using WSLExtension.Helpers.Distros;
using WSLExtension.Models;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;
using static WSLExtension.DistroDefinitions.KnownDistributionHelper;
using static WSLExtension.Helpers.WslCommandOutputParser;

namespace WSLExtension.Services;

public class WslManager : IWslManager
{
    private readonly IProcessCaller _processCaller;

    private readonly IStringResource _stringResource;

    private readonly PackageHelper _packageHelper = new();

    private readonly object _lock = new();

    private Dictionary<string, KnownDistributionInfo>? _knownDistributionMap;

    public Dictionary<string, HashSet<Process>> DistributionRunningProcessMap { get; private set; } = new();

    public WslManager(IProcessCaller processCaller, IStringResource stringResource)
    {
        _processCaller = processCaller;
        _stringResource = stringResource;
    }

    public async Task<List<WslRegisteredDistribution>> GetAllRegisteredDistributionsAsync()
    {
        _knownDistributionMap ??= await RetrieveKnownDistributionInfoAsync();
        var registeredDistributions = GetRegisteredDistributions();
        var wslComputeSystems = new List<WslRegisteredDistribution>();

        foreach (var distribution in registeredDistributions)
        {
            if (_knownDistributionMap.TryGetValue(distribution.Key, out var knownDistributionInfo))
            {
                distribution.Value.Logo = knownDistributionInfo.Logo;
                distribution.Value.FriendlyName = knownDistributionInfo.FriendlyName;
            }

            wslComputeSystems.Add(new WslRegisteredDistribution(_stringResource, distribution.Value, this));
        }

        return wslComputeSystems;
    }

    public async Task<List<DistributionState>> GetOnlineAvailableDistributionsAsync()
    {
        var task = distros
            .Where(d => registeredDistros.All(r => r.Registration != d.Registration))
            .Select(async d => new DistributionState
            {
                Registration = d.Registration,
                Name = d.Name,
                Logo = await ReadAndEncodeImage(d.Logo),
            });

        var distroArray = await Task.WhenAll(task);

        var processData = _processCaller.CreateProcessWithoutWindow(GetFileNameForProcessLaunch(), ListAllWslDistributionsFromMsStoreArgs);
        if (processData.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to retrieve all available WSL distributions from the Microsoft Store:" +
                $" StdOutput: {processData.StdError}");
        }

        var registeredDistributionsMap = GetRegisteredDistributions();
        var distributionsToListOnCreationPage = new List<DistributionState>();
        _knownDistributionMap ??= await RetrieveKnownDistributionInfoAsync();
        foreach (var distributionName in ParseKnownDistributionsFoundInMsStore(processData.StdOutput))
        {
            if (registeredDistributionsMap.TryGetValue(distributionName, out var distribution))
            {
                continue;
            }

            if (_knownDistributionMap.TryGetValue(distributionName, out var knownDistributionInfo))
            {
                distribution.Value.Logo = knownDistributionInfo.Logo;
                distribution.Value.FriendlyName = knownDistributionInfo.FriendlyName;
            }
        }

        return distroArray.ToList();
    }

    public static async Task<string?> ReadAndEncodeImage(string? logo)
    {
        var uri = new Uri($"ms-appx:///WSLExtension/DistroDefinitions/Assets/{logo}");
        var storageFile = await StorageFile.GetFileFromApplicationUriAsync(uri);
        var randomAccessStream = await storageFile.OpenReadAsync();

        // Convert the stream to a byte array
        var bytes = new byte[randomAccessStream.Size];
        await randomAccessStream.ReadAsync(bytes.AsBuffer(), (uint)randomAccessStream.Size, InputStreamOptions.None);

        return Convert.ToBase64String(bytes);
    }

    public void UnregisterDistribution(string distributionName)
    {
        var arguments = UnregisterDistributionArgs.FormatArgs(distributionName);
        var processData = _processCaller.CreateProcessWithoutWindow(GetFileNameForProcessLaunch(), arguments);

        if (processData.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to unregister the distro {distributionName}: StdOutput: {processData.StdError}");
        }
    }

    public void InstallDistribution(string distributionName)
    {
        var arguments = InstallDistributionArgs.FormatArgs(distributionName);
        var process = _processCaller.CreateProcessWithWindow(GetFileNameForProcessLaunch(), arguments);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to install the distro {distributionName}");
        }
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

    public DistributionState? GetRegisteredDistribution(string distributionName)
    {
        foreach (var registeredDistribution in GetRegisteredDistributions().Values)
        {
            if (distributionName.Equals(registeredDistribution.Name, StringComparison.Ordinal))
            {
                return registeredDistribution;
            }
        }

        return null;
    }

    private Dictionary<string, DistributionState> GetRegisteredDistributions()
    {
        var distributions = new Dictionary<string, DistributionState>();

        var linuxSubSystemKey = CurrentUser.OpenSubKey(WslRegisryLocation, false);

        if (linuxSubSystemKey == null)
        {
            return distributions;
        }

        foreach (var subKeyName in linuxSubSystemKey.GetSubKeyNames())
        {
            var subKey = linuxSubSystemKey.OpenSubKey(subKeyName);

            if (subKey == null)
            {
                continue;
            }

            var distribution = BuildDistribution(subKey);
            distributions.Add(distribution.Name, distribution);
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
