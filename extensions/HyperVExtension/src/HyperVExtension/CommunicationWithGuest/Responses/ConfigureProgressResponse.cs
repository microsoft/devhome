// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using HyperVExtension.HostGuestCommunication;
using HyperVExtension.Models.VMGalleryJsonToClasses;

namespace HyperVExtension.CommunicationWithGuest;

/// <summary>
/// Class used to handle progress response for Configure request (RequestType = Configure).
/// </summary>
internal sealed class ConfigureProgressResponse : ResponseBase
{
    public ConfigureProgressResponse(IResponseMessage responseMessage, JsonNode jsonData)
        : base(responseMessage, jsonData)
    {
        ProgressCounter = GetRequiredUintValue(nameof(ProgressCounter));

        // Calling JsonSerializer.Deserialize directly on JasonNode fails (why?), but deserializing
        // from the original string works.
        var configurationSetChangeDataNode = (string?)jsonData[nameof(ConfigurationSetChangeData)];
        if (configurationSetChangeDataNode == null)
        {
            // TODO: we may want to proceed without data and handle it later. That way calling code will know that
            // Configure operation is completed.
            throw new JsonException($"Missing {nameof(ConfigurationSetChangeData)} in JSON data.");
        }

        var configurationSetChangeData = JsonSerializer.Deserialize<ConfigurationSetChangeData>(configurationSetChangeDataNode, SourceGenerationContextConfiguration.Default.ConfigurationSetChangeData);
        if (configurationSetChangeData == null)
        {
            throw new JsonException($"Failed to deserialize {nameof(ConfigurationSetChangeData)} from JSON data.");
        }

        ProgressData = configurationSetChangeData;
    }

    public ConfigurationSetChangeData ProgressData { get; }

    public uint ProgressCounter { get; }
}
