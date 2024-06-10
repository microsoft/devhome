// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.WindowsPackageManager.Contracts;
using DevHome.Services.WindowsPackageManager.Models;

namespace DevHome.Services.WindowsPackageManager.Services;

internal sealed class DesiredStateConfiguration : IDesiredStateConfiguration
{
    private readonly IDSCOperations _dscOperations;
    private readonly IWinGetDeployment _winGetDeployment;

    public DesiredStateConfiguration(
        IDSCOperations dscOperations,
        IWinGetDeployment winGetDeployment)
    {
        _dscOperations = dscOperations;
        _winGetDeployment = winGetDeployment;
    }

    /// <inheritdoc />
    public async Task<bool> IsUnstubbedAsync() => await _winGetDeployment.IsConfigurationUnstubbedAsync();

    /// <inheritdoc />
    public async Task<bool> UnstubAsync() => await _winGetDeployment.UnstubConfigurationAsync();

    public async Task<DSCApplicationResult> ApplyConfigurationAsync(string filePath, Guid activityId) => await _dscOperations.ApplyConfigurationAsync(filePath, activityId);

    public Task<IReadOnlyList<DSCConfigurationUnit>> GetConfigurationUnitDetailsAsync(DSCConfiguration configuration, Guid activityId) => throw new NotImplementedException();

    public Task ValidateConfigurationAsync(string filePath, Guid activityId) => throw new NotImplementedException();
}
