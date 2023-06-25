// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevHome.Dashboard.Helpers;
internal sealed class WidgetCustomState
{
    [JsonPropertyName("host")]
    public string Host { get; set; }
}

[JsonSerializable(typeof(WidgetCustomState))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{
}
