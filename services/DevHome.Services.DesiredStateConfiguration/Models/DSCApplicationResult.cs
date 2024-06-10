// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Management.Configuration;

namespace DevHome.Services.DesiredStateConfiguration.Models;

public class DSCApplicationResult
{
    public ApplyConfigurationSetResult Result
    {
        get;
    }

    public bool Succeeded => Result.ResultCode == null;

    public bool RequiresReboot => Result.UnitResults.Any(result => result.RebootRequired);

    public Exception ResultException => Result.ResultCode;

    public DSCApplicationResult(ApplyConfigurationSetResult result)
    {
        Result = result;
    }
}
