// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.Models;

/// <summary>
/// Represents the user input for a Wsl install and registration operation.
/// </summary>
public sealed class WslInstallationUserInput
{
    /// <summary>
    /// Gets or sets the WSL distribution name for the creation operation.
    /// Note: Do not rename this variable, as Dev Home looks for the name
    /// 'NewEnvironmentName' in the user input Json in order to show the
    /// name of the environment in the UI while its being created in the
    /// CreateComputeSystemOperation object.
    /// </summary>
    public string NewEnvironmentName { get; set; } = string.Empty;

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SelectedDistributionIndex { get; set; }
}
