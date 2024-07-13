// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle requests that have no request type.
/// It creates an error response JSON to send back to the client.
/// </summary>
internal sealed class ErrorNoTypeResponse : IGuestResponse
{
    public ErrorNoTypeResponse(IResponseMessage message)
    {
        Timestamp = DateTime.UtcNow;
        ResponseId = message.ResponseId!;
        RequestId = message.ResponseId!;
    }

    public string RequestId { get; set; }

    public string RequestType { get; set; } = "<unknown>";

    public string ResponseId { get; set; }

    public string ResponseType { get; set; } = "<unknown>";

    public uint Status { get; set; } = 0xFFFFFFFF;

    public string ErrorDescription { get; set; } = "Missing Response or Request type.";

    public uint Version { get; set; } = 1;

    public DateTime Timestamp { get; set; }
}
