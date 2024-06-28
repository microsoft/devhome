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
    public IEnumerable<WslRegisteredDistro> GetAllRegisteredDistributions();

    void UnregisterDistribution(string distributionName);

    Task<List<Distro>> GetOnlineAvailableDistributions();

    void InstallDistribution(string distributionName);

    bool IsWslEnabled { get; }

    List<Distro> Definitions { get; }

    void LaunchDistribution(string distributionName);
}
