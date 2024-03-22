// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

internal sealed class TooManyRequestsResponse : ResponseBase
{
    public TooManyRequestsResponse(string requestId)
        : base(requestId)
    {
        // TODO: Better error story
        Status = Windows.Win32.Foundation.HRESULT.E_FAIL;
        ErrorDescription = "Too many requests in the queue.";
        GenerateJsonData();
    }
}
