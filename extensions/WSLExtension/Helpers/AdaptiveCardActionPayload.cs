// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.Helpers;

internal sealed class AdaptiveCardActionPayload
{
    public string? Id { get; set; }
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AdaptiveCardActionPayload))]
internal sealed partial class AdaptiveCardActionPayloadSourceGenerationContext : JsonSerializerContext
{
}
