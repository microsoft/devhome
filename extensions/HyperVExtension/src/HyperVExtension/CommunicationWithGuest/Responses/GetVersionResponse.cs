// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle response for service version (RequestType = GetVersion).
/// </summary>
internal sealed class GetVersionResponse : ResponseBase
{
    public GetVersionResponse(IResponseMessage responseMessage, JsonNode jsonData)
        : base(responseMessage, jsonData)
    {
    }
}
