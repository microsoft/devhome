// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SampleExtension.Providers;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(NavigationCardData))]
internal sealed partial class NavigationCardSerializerContext : JsonSerializerContext
{
}
