// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Models;

namespace DevHome.Services.DesiredStateConfiguration.Contracts;

public interface IDSC
{
    /// <inheritdoc cref="IDSCDeployment.IsUnstubbedAsync" />
    public Task<bool> IsUnstubbedAsync();

    /// <inheritdoc cref="IDSCDeployment.UnstubAsync" />
    public Task<bool> UnstubAsync();

    /// <inheritdoc cref="IDSCOperations.ApplyConfigurationAsync" />
    public Task<DSCApplicationResult> ApplyConfigurationAsync(IDSCFile file, Guid activityId);

    /// <inheritdoc cref="IDSCOperations.GetConfigurationUnitDetailsAsync" />
    public Task<IReadOnlyList<IDSCUnit>> GetConfigurationUnitDetailsAsync(IDSCFile file);

    /// <inheritdoc cref="IDSCOperations.ValidateConfigurationAsync" />
    public Task ValidateConfigurationAsync(IDSCFile file);
}
