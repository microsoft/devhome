// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage;
using Windows.Storage.Streams;
using WSLExtension.Common;
using WSLExtension.DistroDefinitions;
using WSLExtension.Helpers;
using WSLExtension.Helpers.Distros;
using WSLExtension.Models;

namespace WSLExtension.Services;

public class WslManager : IWslManager
{
    private readonly IProcessCaller _processCaller;
    private readonly IRegistryAccess _registryAccess;
    private readonly IStringResource _stringResource;
    private readonly bool _isWslEnabled;
    private readonly ImmutableList<Distro> _distroDefinitions;

    public WslManager(IProcessCaller processCaller, IRegistryAccess registryAccess, IStringResource stringResource)
    {
        _processCaller = processCaller;
        _registryAccess = registryAccess;
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
            WtProfileGuid = d.WtProfileGuid,
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

    public void Run(string registration, string? wtProfileGuid)
    {
        DistroState.Run(registration, wtProfileGuid, isRoot: false, processCaller: _processCaller);
    }

    public void Terminate(string registration)
    {
        DistroState.Terminate(registration, _processCaller);
    }

    public void Unregister(string registration)
    {
        Management.Unregister(registration, _processCaller);
    }

    public async Task<int> InstallWslDistribution(string registration)
    {
        return await Management.InstallWslDistribution(_processCaller, registration);
    }

    public void InstallWslDistributionDistribution(string registration)
    {
        Management.InstallDistro(_processCaller, registration);
    }
}
