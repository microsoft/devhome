// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevSetupAgent.Test;

using HyperVExtension.DevSetupAgent;
using Microsoft.Win32;

public class TestRegistryChannelSettings : IRegistryChannelSettings
{
    public string FromHostRegistryKeyPath => @"TEST\External";

    public string ToHostRegistryKeyPath => @"TEST\Guest";

    public RegistryHive RegistryHive => RegistryHive.CurrentUser;
}
