// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent.AppManagement;

/// <summary>
/// The result of an installation.
/// </summary>
/// <remarks>
/// This exists only because it is easier to have this ad-hoc type that
/// we can project with CsWinRT than to make everything else fit on
/// to CsWinRT requirements.
/// </remarks>
public sealed class ElevatedInstallResult
{
    public bool InstallAttempted
    {
        get; set;
    }

    public bool InstallSucceeded
    {
        get; set;
    }

    public bool RebootRequired
    {
        get; set;
    }
}
