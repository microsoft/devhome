// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System.Text.Json;

namespace HyperVExtension.DevSetupAgent;

internal class TooManyRequestsResponse : ErrorResponseBase
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
