// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.DistributionDefinitions;

/// <summary>
/// Represents a definition of a WSL distribution. This metadata is used
/// to deserialize the WSL DistributionInfo.json file located in
/// <see cref="Constants.KnownDistributionsWebJsonLocation"/>. It is also
/// used to deserialize the WSL definitions located in the local
/// <see cref="Constants.KnownDistributionsLocalYamlLocation"/> file.
/// </summary>
public class DistributionDefinition
{
    public string FriendlyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string LogoFile { get; set; } = string.Empty;

    public string Base64StringLogo { get; set; } = string.Empty;

    public string? WindowsTerminalProfileGuid { get; set; }

    public string StoreAppId { get; set; } = string.Empty;

    [JsonPropertyName("Amd64")]
    public bool IsAmd64Supported { get; set; }

    [JsonPropertyName("Arm64")]
    public bool IsArm64Supported { get; set; }

    public string PackageFamilyName { get; set; } = string.Empty;

    public string Publisher { get; set; } = string.Empty;

    public bool IsSameDistribution(string name)
    {
        // Both name and friendly name should not be empty or null in the WSL distribution
        // json.
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(FriendlyName))
        {
            return false;
        }

        // For distributions retrieved from the WSL distribution Json, both the name and
        // friendly names are unique.
        return Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            FriendlyName.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
