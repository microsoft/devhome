// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.DistributionDefinitions;

public class DistributionDefinitions
{
    [JsonPropertyName("Distributions")]
    public List<DistributionDefinition> Values { get; set; } = new();
}
