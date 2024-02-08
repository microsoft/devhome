// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevSetupAgent.Test;

using HyperVExtension.DevSetupAgent;
using Microsoft.Win32;

public class TestRegistryChannelSettings : IRegistryChannelSettings
{
    public string FromHostRegistryKeyPath => @"TEST\External";

    public string ToHostRegistryKeyPath => @"TEST\Guest";

    public RegistryKey RegistryHive => Registry.CurrentUser;
}
