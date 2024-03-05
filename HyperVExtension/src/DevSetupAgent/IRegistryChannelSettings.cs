// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface providing registry channel settings.
/// </summary>
public interface IRegistryChannelSettings
{
    string FromHostRegistryKeyPath { get; }

    string ToHostRegistryKeyPath { get; }

    RegistryHive RegistryHive { get; }
}
