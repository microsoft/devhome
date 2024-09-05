// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using WSLExtension.Models;

namespace WSLExtension.DistributionDefinitions;

/// <summary>
/// Used when deserializing json file at <see cref="Constants.KnownDistributionsWebJsonLocation"/>
/// </summary>
public class DistributionDefinitions
{
    [JsonPropertyName("Distributions")]
    public List<DistributionDefinition> Values { get; set; } = new();
}

// Uses .NET's JSON source generator support for serializing / deserializing to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(DistributionDefinitions))]
internal sealed partial class DistributionDefinitionsSourceGenerationContext : JsonSerializerContext
{
}
