// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using WSLExtension.Models;

namespace WSLExtension.Contracts;

public interface IWslManager
{
    /// <summary> Gets a list of all registered WSL distributions on the machine.</summary>
    /// <returns> A list of registered WSL distributions.</returns>
    public Task<List<WslRegisteredDistribution>> GetAllRegisteredDistributionsAsync();

    public Task<List<WslDistributionInfo>> GetAllDistributionInfoFromGitHubAsync();

    public Task<WslDistributionInfo?> GetRegisteredDistributionAsync(string distributionName);

    void UnregisterDistribution(string distributionName);

    public WslProcessData InstallDistribution(string distributionName, DataReceivedEventHandler stdOutputHandler, DataReceivedEventHandler stdErrorHandler);

    void LaunchDistribution(string distributionName);
}
