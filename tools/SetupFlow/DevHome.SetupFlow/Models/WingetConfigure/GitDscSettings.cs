// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using Newtonsoft.Json;

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
