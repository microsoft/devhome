// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle unsupported requests.
/// </summary>
internal sealed class ErrorUnsupportedResponse : ResponseBase
{
    public ErrorUnsupportedResponse(IResponseMessage responseMessage, JsonNode jsonData)
        : base(responseMessage, jsonData)
    {
    }
}
