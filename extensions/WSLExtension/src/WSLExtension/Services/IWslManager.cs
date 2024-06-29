// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLExtension.Models;

namespace WSLExtension.Services;

public interface IWslManager
{
    /// <summary> Gets a list of all registered WSL distributions on the machine.</summary>
    /// <returns> A list of registered WSL distributions.</returns>
    public Task<List<WslRegisteredDistribution>> GetAllRegisteredDistributionsAsync();

    public Task<List<DistributionState>> GetOnlineAvailableDistributionsAsync();

    void UnregisterDistribution(string distributionName);

    void InstallDistribution(string distributionName);

    void LaunchDistribution(string distributionName);
}
