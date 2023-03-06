// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.RepoConfig.Models;
internal class Common
{
    /// <summary>
    /// Used to figure out how the user specified their cloning location.
    /// Used in code-behind to figure out what control to read from.
    /// </summary>
    internal enum CloneLocationSelectionMethod
    {
        None,
        LocalPath,
        DevVolume,
    }

    /// <summary>
    /// Used to keep track of what page the user is on.
    /// Used in code-behind to keep track of what information to use when determining if the Primary Button is enabled.
    /// </summary>
    internal enum CurrentPage
    {
        AddViaUrl,
        AddViaAccount,
        Repositories,
    }
}
