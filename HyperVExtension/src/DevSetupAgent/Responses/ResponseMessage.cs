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
        ResponseId = requestId;
        ResponseData = responseData;
    }

    public string ResponseId { get; set; }

    public string ResponseData { get; set; }
}
