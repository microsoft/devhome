// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using HyperVExtension.CommunicationWithGuest;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle invalid requests (for example an exception while parsing request JSON).
/// It creates an error response to send back to the client.
/// </summary>
internal sealed class ErrorResponse : IGuestResponse
{
    public ErrorResponse(IResponseMessage responseMessage)
    {
        ResponseId = responseMessage.ResponseId!;
        Timestamp = DateTime.UtcNow;
    }

    public string RequestId { get; set; } = "<unknown>";

    public string RequestType { get; set; } = "<unknown>";

    public string ResponseId { get; set; }

    public string ResponseType { get; set; } = "ErrorNoData";

    public uint Status { get; set; } = 0x80004005; // E_FAIL

    public string ErrorDescription { get; set; } = "Missing Request data.";

    public uint Version { get; set; } = 1;

    public DateTime Timestamp { get; set; }
}
