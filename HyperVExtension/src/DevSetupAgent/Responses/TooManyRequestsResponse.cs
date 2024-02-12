// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;

namespace HyperVExtension.DevSetupAgent;

internal sealed class TooManyRequestsResponse : ErrorResponseBase
{
    public TooManyRequestsResponse(string requestId)
        : base(requestId)
    {
        // TODO: Better error story
        Status = 0xFFFFFFFF;
        GenerateJsonData();
    }

    public override string Error => "Too many requests in the queue.";
}
