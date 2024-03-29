// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards.Rendering.WinUI3;
using DevHome.Common.Environments.Models;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationProviderChangedData
{
    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public CreationProviderChangedData(ComputeSystemProviderDetails computeSystemProviderDetails)
    {
        ProviderDetails = computeSystemProviderDetails;
    }
}
