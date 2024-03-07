// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have unsupported request type.
/// It creates an error response JSON to send back to the client.
/// </summary>
internal sealed class ErrorUnsupportedRequestResponse : ResponseBase
{
    public ErrorUnsupportedRequestResponse(string requestId, string requestType)
        : base(requestId, requestType)
    {
        Status = Windows.Win32.Foundation.HRESULT.E_FAIL;
        ErrorDescription = "Missing Request type.";
        GenerateJsonData();
    }
}
