// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Response message data.
/// </summary>
internal struct ResponseMessage : IResponseMessage
{
    public ResponseMessage(string communicationId, string responseData)
    {
        CommunicationId = communicationId;
        ResponseData = responseData;
    }

    public string CommunicationId { get; set; }

    public string ResponseData { get; set; }
}
