// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCApplicationResult
{
    /// <summary>
    /// Gets the configuration set that was applied.
    /// </summary>
    IDSCSet AppliedSet { get; }

    /// <summary>
    /// Gets a value indicating whether the application of the configuration
    /// file succeeded.
    /// </summary>
    bool Succeeded { get; }

    /// <summary>
    /// Gets a value indicating whether a reboot is required to complete the
    /// application of the configuration file.
    /// </summary>
    bool RequiresReboot { get; }

    /// <summary>
    /// Gets the exception that occurred during the application of the
    /// configuration file.
    /// </summary>
    Exception ResultException { get; }

    /// <summary>
    /// Gets the results of the individual units in the configuration file.
    /// </summary>
    public IReadOnlyList<IDSCApplicationUnitResult> UnitResults { get; }
}
