// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.Models;

public class WindowsIdentityService : IWindowsIdentityService
{
    public WindowsIdentityWrapper GetCurrentWindowsIdentity()
    {
        return new WindowsIdentityWrapper();
    }
}
