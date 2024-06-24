// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services;

public interface IQuickStartProjectService
{
    public Task<List<QuickStartProjectProvider>> GetQuickStartProjectProvidersAsync();
}
