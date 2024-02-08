// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Win32;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Interface providing registry channel settings.
/// </summary>
public interface IRegistryChannelSettings
{
    string FromHostRegistryKeyPath { get; }

    string ToHostRegistryKeyPath { get; }

    RegistryKey RegistryHive { get; }
}
