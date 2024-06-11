// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSCUnit
{
    public string Type { get; }

    public string Id { get; }

    public string Description { get; }

    public string Intent { get; }

    public IList<string> Dependencies { get; }

    public IList<KeyValuePair<string, string>> Settings { get; }

    public IList<KeyValuePair<string, string>> Metadata { get; }

    public IDSCUnitDetails Details { get; }
}
