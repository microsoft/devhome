﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.Models;
internal sealed class Common
{
    /// <summary>
    /// Used to keep track of what page the user is on.
    /// Used in code-behind to keep track of what information to use when determining if the Primary Button is enabled.
    /// </summary>
    internal enum PageKind
    {
        AddViaUrl,
        AddViaAccount,
        Repositories,
    }
}
