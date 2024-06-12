// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.Services.DesiredStateConfiguration.Services;

internal sealed class DSC : IDSC
{
    private readonly IDSCDeployment _dscDeployment;
    private readonly IDSCOperations _dscOperations;

    public DSC(IDSCDeployment dscDeployment, IDSCOperations dscOperations)
    {
        _dscDeployment = dscDeployment;
        _dscOperations = dscOperations;
    }

    /// <inheritdoc/>
    public async Task<bool> IsUnstubbedAsync() => await _dscDeployment.IsUnstubbedAsync();

    /// <inheritdoc/>
    public async Task<bool> UnstubAsync() => await _dscDeployment.UnstubAsync();

    /// <inheritdoc/>
    public async Task<IDSCApplicationResult> ApplyConfigurationAsync(IDSCFile file, Guid activityId) => await _dscOperations.ApplyConfigurationAsync(file, activityId);

    /// <inheritdoc/>
    public async Task<IDSCSet> GetConfigurationUnitDetailsAsync(IDSCFile file) => await _dscOperations.GetConfigurationUnitDetailsAsync(file);

    /// <inheritdoc/>
    public async Task ValidateConfigurationAsync(IDSCFile file) => await _dscOperations.ValidateConfigurationAsync(file);
}
