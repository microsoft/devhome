// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace DevHome.Common.Helpers;

public partial class RestartHelper
{
    [RelayCommand]
    public static void RestartComputer()
    {
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = Environment.SystemDirectory + "\\shutdown.exe",
            Arguments = "-r -t 0",
            Verb = string.Empty,
        };

        var process = new Process
        {
            StartInfo = startInfo,
        };
        process.Start();
    }
}
