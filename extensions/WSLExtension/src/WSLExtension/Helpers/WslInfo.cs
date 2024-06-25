// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WSLExtension.Services;

namespace WSLExtension.Helpers;

public class WslInfo
{
    public static bool IsWslEnabled(IProcessCaller processCaller)
    {
        var helpText = processCaller.CallProcess("wsl", "--help");

        if (helpText.Contains("https://aka.ms/wslinstall"))
        {
            return false;
        }

        return helpText.Contains("--exec");
    }
}
