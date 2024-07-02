// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DevHome.Environments.ViewModels;

// View model representing a compute system provider and its associated compute systems.
public class PerProviderViewModel
{
    public string ProviderName { get; }

    public string DecoratedDevID { get; }

    public ObservableCollection<ComputeSystemCardBase> ComputeSystems { get; }

    public PerProviderViewModel(string providerName, string associatedDevID, List<ComputeSystemCardBase> computeSystems)
    {
        ProviderName = providerName;
        DecoratedDevID = associatedDevID.Length > 0 ? '(' + associatedDevID + ')' : string.Empty;
        ComputeSystems = new ObservableCollection<ComputeSystemCardBase>(computeSystems);
    }
}
