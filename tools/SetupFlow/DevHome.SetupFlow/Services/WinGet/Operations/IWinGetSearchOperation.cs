// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

public interface IWinGetSearchOperation
{
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);
}
