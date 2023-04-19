// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

/// <summary>
/// Class for the result of an individual unit/resource inside <see cref="ConfigureTaskResult"/>
/// </summary>
public sealed class ConfigureUnitTaskResult
{
    public string? UnitName { get; set; }

    public string? Intent { get; set; }

    public bool IsSkipped { get; set; }

    public int HResult { get; set; }
}
