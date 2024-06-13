// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCApplicationUnitResult
{
    /// <summary>
    /// Gets the applied configuration unit.
    /// </summary>
    public IDSCUnit AppliedUnit { get; }

    /// <summary>
    /// Gets the configuration unit error description.
    /// </summary>
    public string ErrorDescription { get; }

    /// <summary>
    /// Gets a value indicating whether the configuration unit requires a reboot.
    /// </summary>
    public bool RebootRequired { get; }

    /// <summary>
    /// Gets a value indicating whether the configuration unit is skipped.
    /// </summary>
    public bool IsSkipped { get; }

    /// <summary>
    /// Gets the HResult of the configuration unit result.
    /// </summary>
    public int HResult { get; }

    /// <summary>
    /// Gets the result source of the configuration unit result.
    /// </summary>
    public ConfigurationUnitResultSource ResultSource { get; }

    /// <summary>
    /// Gets a more detailed error message appropriate for diagnosing the root
    /// cause of an error.
    /// </summary>
    public string Details { get; }
}
