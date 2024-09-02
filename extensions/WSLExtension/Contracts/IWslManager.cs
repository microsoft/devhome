// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.ApplicationModel.Store.Preview.InstallControl;
using WSLExtension.DistributionDefinitions;
using WSLExtension.Models;

namespace WSLExtension.Contracts;

/// <summary>
/// Used to interact between the WSL dev environment compute systems and the WSL mediator.
/// </summary>
public interface IWslManager
{
    public event EventHandler<HashSet<string>>? DistributionStateSyncEventHandler;

    /// <summary> Gets a list of all registered WSL distributions on the machine.</summary>
    public Task<List<WslComputeSystem>> GetAllRegisteredDistributionsAsync();

    /// <summary>
    /// Gets a list of objects that each contain metadata about a wsl distribution
    /// that is not currently registered on the machine and is available to install.
    /// </summary>
    public Task<List<DistributionDefinition>> GetAllDistributionsAvailableToInstallAsync();

    /// <summary>
    /// Gets a list of objects that each contain information about a WSL distribution that is already
    /// registered on the machine.
    /// </summary>
    public Task<WslRegisteredDistribution?> GetInformationOnRegisteredDistributionAsync(string distributionName);

    /// <summary>
    /// Unregisters a WSL distribution. This is a wrapper for <see cref="IWslServicesMediator.UnregisterDistribution(string)"/>
    /// </summary>
    void UnregisterDistribution(string distributionName);

    /// <summary> Launches a new WSL distribution.
    /// This is a wrapper for <see cref="IWslServicesMediator.LaunchDistribution"/>
    /// </summary>
    void LaunchDistribution(string distributionName, string? windowsTerminalProfile);

    /// <summary> Installs a new WSL distribution.
    /// This is a wrapper for <see cref="IWslServicesMediator.InstallDistribution(string)"/>
    /// </summary>
    void InstallDistribution(string distributionName);

    /// <summary> Terminates all sessions for a new WSL distribution.
    /// This is a wrapper for <see cref="IWslServicesMediator.TerminateDistribution(string)"/>
    /// </summary>
    void TerminateDistribution(string distributionName);

    /// <summary> Gets a boolean indicating whether the WSL distribution is currently running.
    /// This is a wrapper for <see cref="IWslServicesMediator.IsDistributionRunning(string)"/>
    /// </summary>
    public bool IsDistributionRunning(string distributionName);

    /// <summary> Installs the WSL kernel package from the Microsoft store if it is not already installed. </summary>
    public Task InstallWslKernelPackageAsync(Action<string>? statusUpdateCallback, CancellationToken cancellationToken);

    /// <summary> Provides subscribers with download/installation progress for Microsoft store app installs. </summary>
    public event EventHandler<AppInstallItem>? WslInstallationEventHandler;
}
