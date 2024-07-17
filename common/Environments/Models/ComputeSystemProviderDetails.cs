// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Environments.Models;

public class ComputeSystemProviderDetails
{
    public IExtensionWrapper ExtensionWrapper { get; set; }

    public List<DeveloperIdWrapper> DeveloperIds { get; set; }

    public ComputeSystemProvider ComputeSystemProvider { get; set; }

    public ComputeSystemProviderDetails(IExtensionWrapper extension, ComputeSystemProvider provider, List<DeveloperIdWrapper> developerId)
    {
        ExtensionWrapper = extension;
        ComputeSystemProvider = provider;
        DeveloperIds = developerId;
    }
}
