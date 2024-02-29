// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace DevHome.SetupFlow.Models;

[Flags]
public enum WinGetPackageUriParameters
{
    None = 0,
    Version = 1 << 0,

    // Add all parameters here
    All = Version,
}
