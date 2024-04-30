// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle invalid responses (for example an exception while parsing request JSON).
/// This should only happen in case of a programming error that we'd need to investigate.
/// </summary>
internal class ErrorResponse : IGuestResponse
{
    public ErrorResponse(IResponseMessage? responseMessage)
    {
        Timestamp = DateTime.UtcNow;
        if ((responseMessage != null) && (responseMessage.ResponseData != null))
        {
            ErrorDescription = $"Missing response data. Response data: '{responseMessage.ResponseData}'";
        }
        else
        {
            ErrorDescription = $"Missing response data.";
        }
    }

    public string RequestId { get; set; } = "<unknown>";

    public string RequestType { get; set; } = "<unknown>";

    public string ResponseId { get; set; } = "<unknown>";

    public string ResponseType { get; set; } = "<unknown>";

    public uint Status { get; set; } = 0x80004005; // E_FAIL

    public string ErrorDescription { get; set; }

    public uint Version { get; set; } = 1;

    public DateTime Timestamp { get; set; }
}
