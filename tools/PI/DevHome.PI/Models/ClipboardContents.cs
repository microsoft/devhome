// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DevHome.PI.Models;

public class ClipboardContents
{
    public string Raw { get; set; } = string.Empty;

    public string Hex { get; set; } = string.Empty;

    public string Dec { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Help { get; set; } = string.Empty;

    public void Clear()
    {
        Raw = string.Empty;
        Hex = string.Empty;
        Dec = string.Empty;
        Code = string.Empty;
        Help = string.Empty;
    }
}
