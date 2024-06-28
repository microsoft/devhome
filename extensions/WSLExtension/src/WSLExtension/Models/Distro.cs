// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WSLExtension.Models;

public class Distro
{
    public Distro()
    {
    }

    public Distro(string registration)
    {
        Registration = registration;
    }

    public string? Logo { get; set; }

    public string? Name { get; set; }

    public string Registration { get; set; } = null!;

    public bool? Running { get; set; }

    public bool? DefaultDistro { get; set; }

    public bool? Version2 { get; set; }

    public bool? HasArm64Version { get; set; }

    public string? WindowsTerminalProfileGuid { get; set; }
}
