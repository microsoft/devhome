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
    /// <summary> Gets a list of Hyper-V virtual machines.</summary>
    /// <returns> A list of virtual machines.</returns>
    public IEnumerable<WslRegisteredDistro> GetAllRegisteredDistros();

    void Run(string registration, string? wtProfileGuid);

    void Terminate(string registration);

    void Unregister(string registration);

    Task<List<Distro>> GetOnlineAvailableDistros();

    Task<int> InstallWsl(string registration);

    void InstallWslDistribution(string registration);

    bool IsWslEnabled { get; }

    List<Distro> Definitions { get; }
}
