// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        SearchFields,
    }
}
