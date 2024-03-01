// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Services;

namespace DevHome.Common.Environments.Helpers;

public static class StringResourceHelper
{
    private static readonly StringResource _stringResource = new("DevHome.Common/Resources");
    private const string ComputeSystemCpu = "ComputeSystemCpu";
    private const string ComputeSystemAssignedMemory = "ComputeSystemAssignedMemory";
    private const string ComputeSystemUptime = "ComputeSystemUptime";
    private const string ComputeSystemStorage = "ComputeSystemStorage";
    private const string ComputeSystemUnknownWithColon = "ComputeSystemUnknownWithColon";

    public static string GetResource(string key, params object[] args)
    {
        return _stringResource.GetLocalized(key, args);
    }
}
