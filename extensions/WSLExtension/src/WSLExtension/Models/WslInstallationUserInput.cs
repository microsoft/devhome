// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace WSLExtension.Models;

/// <summary>
/// Represents the user input for a VM gallery creation operation.
/// </summary>
public sealed class WslInstallationUserInput
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SelectedDistroListIndex { get; set; }
}
