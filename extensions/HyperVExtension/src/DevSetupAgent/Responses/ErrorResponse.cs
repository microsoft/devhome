// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have no request type.
/// It creates an error response JSON to send back to the client.
/// </summary>
internal sealed class ErrorResponse : ResponseBase
{
    public ErrorResponse(string requestId, Exception? error)
        : base(requestId)
    {
        if (error != null)
        {
            ErrorDescription = error.Message;
            Status = (uint)error.HResult;
        }
        else
        {
            ErrorDescription = "Missing Request data.";
            Status = Windows.Win32.Foundation.HRESULT.E_FAIL;
        }

        GenerateJsonData();
    }
}
