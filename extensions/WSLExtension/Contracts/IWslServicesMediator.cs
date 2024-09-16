// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.ApplicationModel;
using WSLExtension.Models;

namespace WSLExtension.Contracts;

/// <summary>
/// Used to interact between the WSL service on the machine and
/// the wsl extension itself.
/// </summary>
public interface IWslServicesMediator
{
    /// <summary>
    /// Gets a set of all currently running distributions. Note: each WSL distribution
    /// name is unique.
    /// </summary>
    public HashSet<string> GetAllNamesOfRunningDistributions();

    /// <summary>
    /// Gets a list of the registered distributions on the machine.
    /// </summary>
    public List<WslRegisteredDistribution> GetAllRegisteredDistributions();

    /// <summary>
    /// Unregisters a WSL distribution from the WSL service. Note: This is the same as deleting the
    /// distribution and any of its associated data.
    /// </summary>
    void UnregisterDistribution(string distributionName);

    /// <summary> Launches a new WSL process with the provided distribution. </summary>
    void LaunchDistribution(string distributionName);

    /// <summary> Installs and registers a distribution on the machine. </summary>
    void InstallAndRegisterDistribution(Package distributionPackage);

    /// <summary> Terminates all running WSL sessions for the provided distribution on the machine. </summary>
    void TerminateDistribution(string distributionName);

    /// <summary> Checks whether the provided WSL distribution is currently running </summary>
    /// <returns> True only if the distribution is running. False otherwise.</returns>
    public bool IsDistributionRunning(string distributionName);
}
