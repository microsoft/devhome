// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLDistributionLauncher;

internal sealed class WslLaunchException : Exception
{
    public WslLaunchException(string message)
        : base(message)
    {
    }
}
