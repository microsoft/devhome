// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

/// <summary>
/// Class for an install task result
/// </summary>
public sealed class ElevatedInstallTaskResult : IElevatedTaskResult
{
    public bool TaskAttempted { get; set; }

    public bool TaskSucceeded { get; set; }

    public bool RebootRequired { get; set; }

    public int Status { get; set; }

    public uint InstallerErrorCode { get; set; }

    public int ExtendedErrorCode { get; set; }
}
