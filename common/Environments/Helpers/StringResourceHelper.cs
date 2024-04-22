// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using DevHome.Common.Services;
using Serilog;

namespace DevHome.Common.Environments.Helpers;

public static class StringResourceHelper
{
    private static readonly StringResource _stringResource = new("DevHome.Common.pri", "DevHome.Common/Resources");
    private const string ComputeSystemCpu = "ComputeSystemCpu";
    private const string ComputeSystemAssignedMemory = "ComputeSystemAssignedMemory";
    private const string ComputeSystemUptime = "ComputeSystemUptime";
    private const string ComputeSystemStorage = "ComputeSystemStorage";
    private const string ComputeSystemUnknownWithColon = "ComputeSystemUnknownWithColon";
    public const string UserNotInHyperAdminGroupButton = "UserNotInHyperAdminGroupButton";
    public const string UserNotInHyperAdminGroupMessage = "UserNotInHyperAdminGroupMessage";

    public static string GetResource(string key, params object[] args)
    {
        try
        {
            return _stringResource.GetLocalized(key, args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to get resource for key {key}.");
            return key;
        }
    }
}
