// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Response message data.
/// </summary>
internal struct RequestMessage : IRequestMessage
{
    public RequestMessage(string requestId, string requestData)
    {
        RequestId = requestId;
        RequestData = requestData;
    }

    public string RequestId { get; set; }

    public string RequestData { get; set; }
}
