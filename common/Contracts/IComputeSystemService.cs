// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;

namespace DevHome.Common.Contracts.Services;

public interface IComputeSystemService
{
    public Task<List<ComputeSystemProviderDetails>> GetComputeSystemProvidersAsync();
}
