// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class that represents a broken response from Hyper-V with request or response type.
/// This should only happen in case of a programming error that we'd need to investigate.
/// </summary>
internal sealed class ErrorNoTypeResponse : ErrorResponse
{
    public ErrorNoTypeResponse(IResponseMessage? responseMessage)
        : base(responseMessage)
    {
        if ((responseMessage != null) && (responseMessage.ResponseData != null))
        {
            ErrorDescription = $"Missing Response or Request type. Response data: '{responseMessage.ResponseData}'";
        }
        else
        {
            ErrorDescription = $"Missing Response or Request type.";
        }
    }
}
