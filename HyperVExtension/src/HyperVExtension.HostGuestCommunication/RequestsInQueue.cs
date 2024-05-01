// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.HostGuestCommunication;

public class RequestsInQueue
{
    public RequestsInQueue(string communicationId, string requestId)
    {
        CommunicationId = communicationId;
        RequestId = requestId;
    }

    public string CommunicationId { get; set; }

    public string RequestId { get; set; }
}
