// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle response for IsUserLoggedInRequest.
/// </summary>
internal sealed class IsUserLoggedInResponse : ResponseBase
{
    public IsUserLoggedInResponse(IResponseMessage responseMessage, JsonNode jsonData)
        : base(responseMessage, jsonData)
    {
        IsUserLoggedIn = GetRequiredBoolValue(nameof(IsUserLoggedIn));
    }

    public bool IsUserLoggedIn { get; internal set; }

    public List<string> LoggedInUsers { get; internal set; } = new List<string>();
}
