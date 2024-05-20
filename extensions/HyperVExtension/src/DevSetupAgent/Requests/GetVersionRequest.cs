// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle request for service version (RequestType = GetVersion).
/// </summary>
internal sealed class GetVersionRequest : RequestBase
{
    public const string RequestTypeId = "GetVersion";

    public GetVersionRequest(IRequestContext requestContext)
        : base(requestContext)
    {
    }

    public override bool IsStatusRequest => true;

    public override IHostResponse Execute(IProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        return new GetVersionResponse(RequestId);
    }
}
