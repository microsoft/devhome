// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DevHome.Common.Environments.Models;

namespace DevHome.SetupFlow.Models.Environments;

/// <summary>
/// Data payload for when the <see cref="Microsoft.Windows.DevHome.SDK.IExtensionAdaptiveCardSession2">
/// session Ends. This data is used to send the user input from an adaptive card session back to an object
/// that subscribes to the <see cref="Microsoft.Windows.DevHome.SDK.IExtensionAdaptiveCardSession2.Stopped">
/// event.
/// </summary>
public class CreationAdaptiveCardSessionEndedData
{
    /// <summary>
    /// Gets the JSON string of the user input from the adaptive card session
    /// </summary>
    public string UserInputResultJson { get; private set; }

    /// <summary>
    /// Gets the provider details for the compute system provider. <see cref="ComputeSystemProviderDetails"/>
    /// </summary>
    public ComputeSystemProviderDetails ProviderDetails { get; private set; }

    public CreationAdaptiveCardSessionEndedData(string userInputResultJson, ComputeSystemProviderDetails providerDetails)
    {
        UserInputResultJson = userInputResultJson;
        ProviderDetails = providerDetails;
    }
}
