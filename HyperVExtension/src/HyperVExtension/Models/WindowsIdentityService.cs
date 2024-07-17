// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperVExtension.Models;

public class WindowsIdentityService : IWindowsIdentityService
{
    public WindowsIdentityWrapper GetCurrentWindowsIdentity()
    {
        return new WindowsIdentityWrapper();
    }
}
