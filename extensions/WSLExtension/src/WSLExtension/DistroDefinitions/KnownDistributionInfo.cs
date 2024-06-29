// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.DistroDefinitions;

public class KnownDistributionInfo
{
    public string FriendlyName { get; set; } = string.Empty;

    public string DistributionName { get; set; } = string.Empty;

    public string LogoAssetName { get; set; } = string.Empty;

    public string Base64StringLogo { get; set; } = string.Empty;

    public string? WindowsTerminalProfileGuid { get; set; }
}
