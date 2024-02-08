// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json.Nodes;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle request for service version (RequestType = GetVersion).
/// </summary>
internal class GetVersionRequest : RequestBase
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
