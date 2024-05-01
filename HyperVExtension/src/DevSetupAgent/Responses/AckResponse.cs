// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Special dummy response class that doesn't generate any data to return.
/// AckRequest is used to acknowledge the receipt of a request and delete sent messages.
/// Nothing to send back in this case.
/// </summary>
internal sealed class AckResponse : ResponseBase
{
    public AckResponse(string requestId)
        : base(requestId, AckRequest.RequestTypeId)
    {
        SendResponse = false;
    }
}
