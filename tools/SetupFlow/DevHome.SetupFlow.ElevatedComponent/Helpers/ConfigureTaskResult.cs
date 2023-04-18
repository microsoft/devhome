// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;
public sealed class ConfigureTaskResult : ITaskResult
{
    public bool TaskAttempted { get; set; }

    public bool TaskSucceeded { get; set; }

    public bool RebootRequired { get; set; }

    public IReadOnlyList<ConfigureUnitTaskResult>? UnitResults { get; set; }
}
