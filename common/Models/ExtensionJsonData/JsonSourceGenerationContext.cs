// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Json.Schema;

namespace DevHome.Common.Models.ExtensionJsonData;

/// <summary>
/// Used to generate C# classes from the extension json and extension json schema.
/// .Net 8 requires a JsonSerializerContext to be used with the JsonSerializerOptions when
/// deserializing JSON into objects.
/// See : https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0
/// for more information
/// </summary>
[JsonSerializable(typeof(LocalizedProperties))]
[JsonSerializable(typeof(ProviderSpecificProperty))]
[JsonSerializable(typeof(Properties))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(DevHomeExtensionJsonData))]
[JsonSerializable(typeof(DevHomeExtension))]
[JsonSerializable(typeof(JsonSchema))]
[JsonSerializable(typeof(EvaluationResults))]
public partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
