// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using DevHome.Services.DesiredStateConfiguration.Contracts;

namespace DevHome.Services.DesiredStateConfiguration.Services;

internal sealed class DSC : IDSC
{
    private readonly IDSCDeployment _dscDeployment;

    public DSC(IDSCDeployment dscDeployment)
    {
        _dscDeployment = dscDeployment;
    }

    public async Task<bool> IsUnstubbedAsync() => await _dscDeployment.IsUnstubbedAsync();

    public async Task<bool> UnstubAsync() => await _dscDeployment.UnstubAsync();
}
