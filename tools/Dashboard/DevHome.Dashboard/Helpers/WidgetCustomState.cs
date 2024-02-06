// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevHome.Dashboard.Helpers;

internal sealed class WidgetCustomState
{
    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; } = -1;
}

[JsonSerializable(typeof(WidgetCustomState))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{
}
