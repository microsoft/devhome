// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.HostGuestCommunication;

// Helper class to return DevSetupAgent state to the client.
public class StateData
{
    public StateData(List<RequestsInQueue> requestsInQueue)
    {
        RequestsInQueue = requestsInQueue;
    }

    public List<RequestsInQueue> RequestsInQueue { get; private set; }
}
