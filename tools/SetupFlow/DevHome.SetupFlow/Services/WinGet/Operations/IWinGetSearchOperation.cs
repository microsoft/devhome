// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using DevHome.SetupFlow.Models;

namespace DevHome.SetupFlow.Services.WinGet.Operations;

internal interface IWinGetSearchOperation
{
    /// <summary>
    /// Search for packages in this catalog.
    /// Equivalent to <c>"winget search --query {query} --source {this}"</c>
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum number of results to return. Use 0 for infinite results</param>
    /// <returns>List of winget package matches</returns>
    public Task<IList<IWinGetPackage>> SearchAsync(string query, uint limit);
}
