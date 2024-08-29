// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DevHome.Helpers;

internal sealed class NavConfig
{
    [JsonPropertyName("navMenu")]
    public NavMenu NavMenu { get; set; }

    [JsonPropertyName("experimentalFeatures")]
    public ExperimentalFeatures[] ExperimentFeatures { get; set; }
}

internal sealed class NavMenu
{
    [JsonPropertyName("groups")]
    public Group[] Groups { get; set; }
}

internal sealed class Group
{
    [JsonPropertyName("identity")]
    public string Identity { get; set; }

    [JsonPropertyName("tools")]
    public Tool[] Tools { get; set; }
}

internal sealed class Tool
{
    [JsonPropertyName("identity")]
    public string Identity { get; set; }

    [JsonPropertyName("assembly")]
    public string Assembly { get; set; }

    [JsonPropertyName("viewFullName")]
    public string ViewFullName { get; set; }

    [JsonPropertyName("viewModelFullName")]
    public string ViewModelFullName { get; set; }

    [JsonPropertyName("iconFontFamily")]
    public string IconFontFamily { get; set; } = "SymbolThemeFontFamily";

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("experimentalFeatureIdentity")]
    public string ExperimentalFeatureIdentity { get; set; }
}

internal sealed class ExperimentalFeatures
{
    [JsonPropertyName("identity")]
    public string Identity { get; set; }

    [JsonPropertyName("enabledByDefault")]
    public bool EnabledByDefault { get; set; }

    [JsonPropertyName("needsFeaturePresenceCheck")]
    public bool NeedsFeaturePresenceCheck { get; set; }

    [JsonPropertyName("buildTypeOverrides")]
    public BuildTypeOverrides[] BuildTypeOverrides { get; set; }

    [JsonPropertyName("openPage")]
    public OpenPage OpenPage { get; set; }
}

internal sealed class BuildTypeOverrides
{
    [JsonPropertyName("buildType")]
    public string BuildType { get; set; }

    [JsonPropertyName("enabledByDefault")]
    public bool EnabledByDefault { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}

internal sealed class OpenPage
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("parameter")]
    public string Parameter { get; set; }
}

// Uses .NET's JSON source generator support for serializing / deserializing NavConfig to get some perf gains at startup.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(NavConfig))]
internal sealed partial class SourceGenerationContext : JsonSerializerContext
{
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
