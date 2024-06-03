// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// Represents settings for a GitDsc resource.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// See: https://github.com/microsoft/devhome/blob/main/sampleConfigurations/DscResources/GitDsc/CloneWingetRepository.yaml
/// </summary>
public class GitDscSettings : WinGetConfigSettingsBase
{
    public string HttpsUrl { get; set; }

    public string RootDirectory { get; set; }
}
