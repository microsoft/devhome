// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

/// <summary>
/// Represents a set of Desired State Configuration (DSC) resources.
/// </summary>
public interface IDSCSet
{
    /// <summary>
    /// Gets the identifier used to uniquely identify the instance of a
    /// configuration set on the system.
    /// </summary>
    public Guid InstanceIdentifier { get; }

    /// <summary>
    /// Gets the name of the set; if from a file this could be the file name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the configuration units that are part of this set.
    /// </summary>
    public IReadOnlyList<IDSCUnit> Units { get; }
}
