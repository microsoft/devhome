// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace DevHome.Common.Models.ExtensionJsonData;

/// <summary>
/// Used to generate the source code for the classes that we deserialize Json to objects for the DevBox feature.
/// .Net 8 requires a JsonSerializerContext to be used with the JsonSerializerOptions when
/// deserializing JSON into objects.
/// See : https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation?pivots=dotnet-8-0
/// for more information
/// </summary>
[JsonSerializable(typeof(ResourceProperties))]
[JsonSerializable(typeof(ProviderSpecificProperty))]
[JsonSerializable(typeof(Properties))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(DevHomeExtensionJsonData))]
[JsonSerializable(typeof(DevHomeExtension))]
public partial class JsonSourceGenerationContext : JsonSerializerContext
{
}
