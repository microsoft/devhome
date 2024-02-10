// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle request for service version (RequestType = GetVersion).
/// </summary>
internal sealed class GetVersionRequest : RequestBase
{
    public GetVersionRequest(IRequestMessage requestMessage, JsonNode jsonData)
        : base(requestMessage, jsonData)
    {
    }

    public override bool IsStatusRequest => true;

    public override string RequestType => "GetVersion";

    public override IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        return new GetVersionResponse(RequestMessage.RequestId!);
    }
}
