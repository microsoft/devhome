// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Contracts.Services;

public interface IComputeSystemService
{
    public Task<List<ComputeSystemProviderDetails>> GetComputeSystemProvidersAsync();
}
