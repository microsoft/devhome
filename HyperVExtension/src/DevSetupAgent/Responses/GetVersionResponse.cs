// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class to generate response to GetVersion request.
/// </summary>
internal sealed class GetVersionResponse : ResponseBase
{
    public GetVersionResponse(string requestId)
        : base(requestId, GetVersionRequest.RequestTypeId)
    {
        GenerateJsonData();
    }
}
