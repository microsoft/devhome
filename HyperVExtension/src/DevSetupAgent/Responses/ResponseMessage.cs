// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Response message data.
/// </summary>
internal struct ResponseMessage : IResponseMessage
{
    public ResponseMessage(string requestId, string responseData)
    {
        CommunicationId = requestId;
        ResponseData = responseData;
    }

    public string CommunicationId { get; set; }

    public string ResponseData { get; set; }
}
