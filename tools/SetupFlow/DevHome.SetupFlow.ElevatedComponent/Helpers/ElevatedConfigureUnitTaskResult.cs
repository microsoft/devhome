// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Microsoft.Management.Configuration;

namespace DevHome.SetupFlow.ElevatedComponent.Helpers;

/// <summary>
/// Class for the result of an individual unit/resource inside <see cref="ElevatedConfigureTaskResult"/>
/// </summary>
public sealed class ElevatedConfigureUnitTaskResult
{
    public string? UnitName { get; set; }

    public string? Id { get; set; }

    public string? Description { get; set; }

    public string? Intent { get; set; }

    public bool IsSkipped { get; set; }

    public int HResult { get; set; }

    public int ResultSource { get; set; }

    public string? Details { get; set; }
}
