// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevHome.Dashboard.Helpers;

internal sealed class WebMessageJson
{
    [JsonPropertyName("devHomeItemUpdated")]
    public string DevHomeItemUpdated { get; set; } = string.Empty;

    [JsonPropertyName("devHomeNumberOfPages")]
    public int DevHomeNumberOfPages { get; set; } = 1;

    [JsonPropertyName("devHomeFormCompleted")]
    public bool DevHomeFormCompleted { get; set; } = false;
}

[JsonSerializable(typeof(WebMessageJson))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{
}
