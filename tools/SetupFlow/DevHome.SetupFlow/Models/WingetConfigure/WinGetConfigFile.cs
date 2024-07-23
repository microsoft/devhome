// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DevHome.SetupFlow.Models.WingetConfigure;

/// <summary>
/// The properties of the WinGet config file.
/// See: https://learn.microsoft.com/windows/package-manager/configuration/create
/// </summary>
public class WinGetConfigFile
{
    public WinGetConfigProperties Properties { get; set; }
}
