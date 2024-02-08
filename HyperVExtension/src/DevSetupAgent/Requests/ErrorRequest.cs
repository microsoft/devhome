// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle invalid requests (for example an exception while parsing request JSON).
/// It creates an error response to send back to the client.
/// </summary>
internal class ErrorRequest : RequestBase
{
    public ErrorRequest(IRequestMessage requestMessage, JsonNode jsonData)
        : base(requestMessage, jsonData)
    {
    }

    public override bool IsStatusRequest => true;

    public override string RequestType => "ErrorNoType";

    public override IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        // TODO: This is a placeholder for a real request.
        return new GetVersionResponse(RequestMessage.RequestId!);
    }
}
