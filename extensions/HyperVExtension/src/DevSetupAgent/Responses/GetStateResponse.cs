// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using HyperVExtension.HostGuestCommunication;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class to generate response to GetState request.
/// Output IDs of requests that are in queue to let client know if VM is busy processing previous requests.
/// </summary>
internal sealed class GetStateResponse : ResponseBase
{
    public GetStateResponse(string requestId, List<RequestsInQueue> requestsInQueue)
        : base(requestId, GetStateRequest.RequestTypeId)
    {
        StateData = new StateData(requestsInQueue);
        GenerateJsonData();
    }

    public StateData StateData { get; private set; }

    protected override void GenerateJsonData()
    {
        base.GenerateJsonData();

        var stateData = JsonSerializer.Serialize(StateData, StateDataSourceGenerationContext.Default.StateData);
        JsonData![nameof(StateData)] = stateData;
    }
}
