// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevHome.Common.DevHomeAdaptiveCards.CardModels;

/// <summary>
/// Represents a choice in an adaptive card choice set. This is the same as the adaptive card choices
/// type in the adaptive card schema with an added subtitle property. This can be updated in the future
/// to add other properties.
/// </summary>
/// <remarks>
/// This class should be kept in sync with the Adaptive card Input.Choice.
/// See: https://www.adaptivecards.io/explorer/Input.Choice.html
/// </remarks>
public class DevHomeChoicesData
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
