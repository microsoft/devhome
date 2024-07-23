// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using DevHome.Services.DesiredStateConfiguration.Contracts;
using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Models;

internal sealed class DSCApplicationResult : IDSCApplicationResult
{
    public DSCApplicationResult(ConfigurationSet appliedSet, ApplyConfigurationSetResult result)
    {
        // Constructor copies all the required data from the out-of-proc COM
        // objects over to the current process. This ensures that we have this
        // information available even if the out-of-proc COM objects are no
        // longer available (e.g. AppInstaller service is no longer running).
        AppliedSet = new DSCSet(appliedSet);
        Succeeded = result.ResultCode == null;
        RequiresReboot = result.UnitResults.Any(result => result.RebootRequired);
        ResultException = result.ResultCode;
        UnitResults = result.UnitResults.Select(unitResult => new DSCApplicationUnitResult(unitResult)).ToList();
    }

    public IDSCSet AppliedSet { get; }

    public bool Succeeded { get; }

    public bool RequiresReboot { get; }

    public Exception ResultException { get; }

    public IReadOnlyList<IDSCApplicationUnitResult> UnitResults { get; }
}
