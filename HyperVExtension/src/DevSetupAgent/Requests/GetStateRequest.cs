// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle request for service state (RequestType = GetState).
/// Returns the number of requests in the queue.
/// </summary>
internal sealed class GetStateRequest : RequestBase
{
    public const string RequestTypeId = "GetState";

    public GetStateRequest(IRequestContext requestContext)
        : base(requestContext)
    {
    }

    public override bool IsStatusRequest => true;

    public override IHostResponse Execute(IProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        return new GetStateResponse(RequestId, RequestContext.RequestsInQueue);
    }
}
