// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Services;

public class DSCOperations : IDSCOperations
{
    public Task<DSCApplicationResult> ApplyConfigurationAsync(string filePath, Guid activityId)
    {
        return null;
    }

    public Task<IReadOnlyList<DSCConfigurationUnit>> GetConfigurationUnitDetailsAsync(DSCConfiguration configuration, Guid activityId)
    {
        return null;
    }

    public Task ValidateConfigurationAsync(string filePath, Guid activityId)
    {
        return null;
    }
}
