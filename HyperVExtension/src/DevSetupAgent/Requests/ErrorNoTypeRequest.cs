// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle requests that have no request type. It creates an error response to send back to the client.
/// </summary>
internal class ErrorNoTypeRequest : IHostRequest
{
    public ErrorNoTypeRequest(IRequestMessage requestMessage, JsonNode jsonData)
    {
        RequestMessage = requestMessage;
        Timestamp = DateTime.UtcNow;
    }

    public IRequestMessage RequestMessage
    {
        get;
    }

    public bool IsStatusRequest => true;

    public virtual string RequestId => "Unknown";

    public string RequestType => "ErrorNoType";

    public DateTime Timestamp { get; }

    public IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        // TODO: This is a placeholder for a real response.
        return new ErrorNoTypeResponse(RequestMessage.RequestId!);
    }
}
