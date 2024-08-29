// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace SampleExtension.Providers;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(SettingsCardData))]
internal sealed partial class SettingsCardSerializerContext : JsonSerializerContext
{
}
