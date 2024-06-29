// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.Models;

/// <summary>
/// Represents the user input for a Wsl install and registration operation.
/// </summary>
public sealed class WslInstallationUserInput
{
    public string NewEnvironmentName { get; set; } = string.Empty;

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SelectedDistroListIndex { get; set; }
}
