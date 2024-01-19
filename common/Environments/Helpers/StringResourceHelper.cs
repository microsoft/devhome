// Copyright(c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevHome.Common.Services;

namespace DevHome.Common.Environments.Helpers;
public static class StringResourceHelper
{
    private static readonly IStringResource _stringResource = new StringResource("DevHome.Common/Resources");
    private const string ComputeSystemCpu = "ComputeSystemCpu";
    private const string ComputeSystemAssignedMemory = "ComputeSystemAssignedMemory";
    private const string ComputeSystemUptime = "ComputeSystemUptime";
    private const string ComputeSystemStorage = "ComputeSystemStorage";
    private const string ComputeSystemUnknownWithColon = "ComputeSystemUnknownWithColon";

    public static string GetResource(string key)
    {
        return _stringResource.GetLocalized(key);
    }
}
