// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.DistributionDefinitions;

public class DistributionDefinition
{
    public string FriendlyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string LogoFile { get; set; } = string.Empty;

    public string Base64StringLogo { get; set; } = string.Empty;

    public string? WindowsTerminalProfileGuid { get; set; }

    public string? StoreAppId { get; set; }

    [JsonPropertyName("Amd64")]
    public bool IsAmd64Supported { get; set; }

    [JsonPropertyName("Arm64")]
    public bool IsArm64Supported { get; set; }

    public string PackageFamilyName { get; set; } = string.Empty;
}
