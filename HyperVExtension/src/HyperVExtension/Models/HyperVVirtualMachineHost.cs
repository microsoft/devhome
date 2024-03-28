// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using HyperVExtension.Helpers;

namespace HyperVExtension.Models;

/// <summary>
/// Represents the Hyper-V virtual machine host. For the Hyper-V Extension this is the local computer.
/// </summary>
public class HyperVVirtualMachineHost
{
    private readonly PsObjectHelper _psObjectHelper;

    public uint LogicalProcessorCount => _psObjectHelper.MemberNameToValue<uint>(HyperVStrings.LogicalProcessorCount);

    public string VirtualHardDiskPath => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.VirtualHardDiskPath) ?? string.Empty;

    public string VirtualMachinePath => _psObjectHelper.MemberNameToValue<string>(HyperVStrings.VirtualMachinePath) ?? string.Empty;

    public HyperVVirtualMachineHost(PSObject psObject)
    {
        _psObjectHelper = new PsObjectHelper(psObject);
    }
}
