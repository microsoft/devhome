// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Contracts.Services;

public interface IComputeSystemService
{
    public Task<Dictionary<IComputeSystemProvider, List<IDeveloperId>>> GetComputeSystemProvidersAsync();
}
