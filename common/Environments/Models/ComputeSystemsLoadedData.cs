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
    public ComputeSystemProviderDetails ProviderDetails { get; set; }

    public Dictionary<DeveloperIdWrapper, ComputeSystemsResult> DevIdToComputeSystemMap { get; set; }

    public ComputeSystemsLoadedData(ComputeSystemProviderDetails providerDetails, Dictionary<DeveloperIdWrapper, ComputeSystemsResult> devIdToComputeSystemMap)
    {
        ProviderDetails = providerDetails;
        DevIdToComputeSystemMap = devIdToComputeSystemMap;
    }
}
