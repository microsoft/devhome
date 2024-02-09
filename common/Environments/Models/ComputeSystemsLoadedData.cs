// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Environments.Models;

/// <summary>
/// Class used to return the results of a ComputeSystem load operation.
/// </summary>
public class ComputeSystemsLoadedData
{
    public KeyValuePair<ComputeSystemProvider, List<DeveloperIdWrapper>> ProviderToDevIdMap { get; set; }

    public List<ComputeSystemsResult> ComputeSystemsResult { get; set; }

    public ComputeSystemsLoadedData(KeyValuePair<ComputeSystemProvider, List<DeveloperIdWrapper>> providerToDevIdMap, List<ComputeSystemsResult> computeSystemsResult)
    {
        ProviderToDevIdMap = providerToDevIdMap;
        ComputeSystemsResult = computeSystemsResult;
    }
}
