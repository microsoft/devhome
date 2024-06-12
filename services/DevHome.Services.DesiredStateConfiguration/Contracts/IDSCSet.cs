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
    public Guid InstanceIdentifier { get; }

    public string Name { get; }

    public IReadOnlyList<IDSCUnit> Units { get; }
}
