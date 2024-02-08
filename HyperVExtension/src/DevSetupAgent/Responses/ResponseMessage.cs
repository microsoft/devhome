// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

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
