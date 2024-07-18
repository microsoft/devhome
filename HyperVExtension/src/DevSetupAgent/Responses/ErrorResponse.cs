// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have no request type.
/// It creates an error response JSON to send back to the client.
/// </summary>
internal sealed class ErrorResponse : ResponseBase
{
    public ErrorResponse(string requestId)
        : base(requestId)
    {
        Status = Windows.Win32.Foundation.HRESULT.E_FAIL;
        ErrorDescription = "Missing Request data.";
        GenerateJsonData();
    }
}
