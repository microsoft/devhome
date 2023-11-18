// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Contracts.Services;

public interface IComputeSystemService
{
    Task<IReadOnlyList<Dictionary<IComputeSystemProvider, List<IDeveloperId>>>> GetComputeSystemProvidersAsync();
}
