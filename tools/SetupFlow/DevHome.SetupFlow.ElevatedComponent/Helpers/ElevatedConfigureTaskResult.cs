// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

/// <summary>
/// Class for a configure task result
/// </summary>
public sealed class ElevatedConfigureTaskResult : IElevatedTaskResult
{
    public bool TaskAttempted { get; set; }

    public bool TaskSucceeded { get; set; }

    public bool RebootRequired { get; set; }

    public IReadOnlyList<ElevatedConfigureUnitTaskResult>? UnitResults { get; set; }
}
