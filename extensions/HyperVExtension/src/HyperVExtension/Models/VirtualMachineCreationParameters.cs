// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using HyperVExtension.Helpers;

namespace HyperVExtension.Models;

public class VirtualMachineCreationParameters
{
    public string Name { get; private set; } = string.Empty;

    // 4Gb in bytes. We use the same default value as the Hyper-V Managers Quick Create feature.
    public long MemoryStartupBytes { get; private set; } = 4096L << 20;

    public string VHDPath { get; private set; } = string.Empty;

    // Virtual machine generation.
    public short Generation => 2;

    public int ProcessorCount { get; private set; }

    public string SecureBoot { get; private set; } = HyperVStrings.ParameterOffState;

    public string EnhancedSessionTransportType { get; private set; } = string.Empty;

    public VirtualMachineCreationParameters(string name, int processorCount, string vhdPath, string secureBoot, string enhanceSessionType)
    {
        Name = name;
        ProcessorCount = processorCount;
        VHDPath = vhdPath;
        SecureBoot = secureBoot.Equals("true", StringComparison.OrdinalIgnoreCase) ? HyperVStrings.ParameterOnState : HyperVStrings.ParameterOffState;
        EnhancedSessionTransportType = enhanceSessionType.Equals(HyperVStrings.ParameterHvSocket, StringComparison.OrdinalIgnoreCase) ? HyperVStrings.ParameterHvSocket : HyperVStrings.ParameterVmBus;
    }
}
