// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle unsupported requests.
/// </summary>
internal sealed class ErrorUnsupportedRequest : RequestBase
{
    public ErrorUnsupportedRequest(IRequestMessage requestMessage, JsonNode jsonData, string requestType)
        : base(requestMessage, jsonData)
    {
        RequestType = requestType;
    }

    public override bool IsStatusRequest => true;

    public override string RequestType { get; }

    public override IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        // TODO: This is a placeholder for a real request.
        return new GetVersionResponse(RequestMessage.RequestId!);
    }
}
