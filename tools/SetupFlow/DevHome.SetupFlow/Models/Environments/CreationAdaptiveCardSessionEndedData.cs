// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Environments.Models;

namespace DevHome.SetupFlow.Models.Environments;

public class CreationAdaptiveCardSessionEndedData
{
    public string UserInputResultJson { get; private set; }

    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public CreationAdaptiveCardSessionEndedData(string userInputResultJson, ComputeSystemProviderDetails providerDetails)
    {
        UserInputResultJson = userInputResultJson;
        ProviderDetails = providerDetails;
    }
}
