// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Win32;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Registry keys provided by Hyper-V Data Exchange Service (KVP).
/// https://learn.microsoft.com/virtualization/hyper-v-on-windows/reference/integration-services#hyper-v-data-exchange-service-kvp
/// https://learn.microsoft.com/previous-versions/windows/it-pro/windows-server-2012-R2-and-2012/dn798287(v=ws.11)
/// "HKLM\SOFTWARE\Microsoft\Virtual Machine\External" contains data pushed to the guest from the host by a user
/// "HKLM\SOFTWARE\Microsoft\Virtual Machine\Guest" contains data created on the guest. This data is available to the host as non-intrinsic data.
/// </summary>
public class RegistryChannelSettings : IRegistryChannelSettings
{
    public string FromHostRegistryKeyPath => @"SOFTWARE\Microsoft\Virtual Machine\External";

    public string ToHostRegistryKeyPath => @"SOFTWARE\Microsoft\Virtual Machine\Guest";

    public RegistryHive RegistryHive => RegistryHive.LocalMachine;
}
