// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevHome.Dashboard.Helpers;
internal class WidgetCustomState
{
    [JsonPropertyName("host")]
    public string Host { get; set; }
}
