// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using HyperVExtension.HostGuestCommunication;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle response for Configure request (RequestType = Configure).
/// </summary>
internal sealed class ConfigureResponse : ResponseBase
{
    public ConfigureResponse(IResponseMessage responseMessage, JsonNode jsonData)
        : base(responseMessage, jsonData)
    {
        // Calling JsonSerializer.Deserialize directly on JasonNode fails (why?), but deserializing
        // from the original string works.
        var applyConfigurationResultNode = (string?)jsonData[nameof(ApplyConfigurationResult)];
        if (applyConfigurationResultNode == null)
        {
            // TODO: we may want to proceed without data and handle it later. That way calling code will know that
            // Configure operation is completed.
            throw new JsonException($"Missing {nameof(ApplyConfigurationResult)} in JSON data.");
        }

        var applyConfigurationResult = JsonSerializer.Deserialize<ApplyConfigurationResult>(applyConfigurationResultNode);
        if (applyConfigurationResult == null)
        {
            throw new JsonException($"Failed to deserialize {nameof(ApplyConfigurationResult)} from JSON data.");
        }

        ApplyConfigurationResult = applyConfigurationResult;
    }

    public ApplyConfigurationResult ApplyConfigurationResult { get; }
}
