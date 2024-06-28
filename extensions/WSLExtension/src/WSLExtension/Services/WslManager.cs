// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
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
using static WSLExtension.Constants;

namespace WSLExtension.Services;

public class WslManager : IWslManager
{
    private readonly IProcessCaller _processCaller;
    private readonly IStringResource _stringResource;
    private readonly bool _isWslEnabled;
    private readonly ImmutableList<Distro> _distroDefinitions;
    private readonly PackageHelper _packageHelper = new();

    private readonly object _lock = new();

    public Dictionary<string, HashSet<Process>> DistributionSessionMap { get; private set; } = new();

    public WslManager(IProcessCaller processCaller, IStringResource stringResource)
    {
        _processCaller = processCaller;
        _stringResource = stringResource;
        _distroDefinitions = [];
        _isWslEnabled = WslInfo.IsWslEnabled(_processCaller);

        var task = Task.Run(DistroDefinitionsManager.ReadDistroDefinitions);
        task.Wait();

        _distroDefinitions = ImmutableList.CreateRange(task.Result);
    }

    // ReSharper disable once ConvertToAutoProperty
    public bool IsWslEnabled => _isWslEnabled;

    public List<Distro> Definitions => _distroDefinitions.ToList();

    public IEnumerable<WslRegisteredDistro> GetAllRegisteredDistributions()
    {
        var distros = DistroDefinitionsManager.Merge(
            _distroDefinitions.ToList(),
            GetInstalledDistros.Execute(_processCaller));

        return distros.Select(d => new WslRegisteredDistro(_stringResource, this)
        {
            DisplayName = d.Name ?? d.Registration,
            SupplementalDisplayName = d.Name != null && d.Name != d.Registration ? d.Registration : string.Empty,
            Running = d.Running,
            Id = d.Registration,
            IsDefault = d.DefaultDistro,
            IsWsl2 = d.Version2,
            Logo = d.Logo,
            WindowsTerminalProfileGuid = d.WindowsTerminalProfileGuid,
        });
    }

    public async Task<List<Distro>> GetOnlineAvailableDistributions()
    {
        var distros = await GetAvailableDistros.Execute(_processCaller);
        var registeredDistros = GetInstalledDistros.Execute(_processCaller);

        var task = distros
            .Where(d => registeredDistros.All(r => r.Registration != d.Registration))
            .Select(async d => new Distro
            {
                Registration = d.Registration,
                Name = d.Name,
                Logo = await ReadAndEncodeImage(d.Logo),
            });

        var distroArray = await Task.WhenAll(task);

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
        var process = _processCaller.CallInteractiveProcess(GetFileNameForProcessLaunch(), arguments);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to unregister the distro {distributionName}");
        }
    }

    public void InstallDistribution(string distributionName)
    {
        var arguments = InstallDistributionArgs.FormatArgs(distributionName);
        var process = _processCaller.CallInteractiveProcess(GetFileNameForProcessLaunch(), arguments);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new WslManagerException($"Failed to install the distro {distributionName}");
        }
    }

    public void LaunchDistribution(string distributionName)
    {
        var arguments = LaunchDistributionArgs.FormatArgs(distributionName);
        var process = _processCaller.CallInteractiveProcess(GetFileNameForProcessLaunch(), arguments);
        lock (_lock)
        {
            if (DistributionSessionMap.TryGetValue(distributionName, out var processList))
            {
                processList.Add(process);
            }
            else
            {
                DistributionSessionMap.Add(distributionName, new HashSet<Process> { process });
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
            foreach (var processSet in DistributionSessionMap.Values)
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
            if (DistributionSessionMap.TryGetValue(distributionName, out var processSet))
            {
                return processSet.Count;
            }

            return 0;
        }
    }
}
