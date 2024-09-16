// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Win32;

namespace DevHome.DevDiagnostics.Helpers;

internal sealed class WERUtils
{
    internal const string LocalWERRegistryKey = "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps";

    // Check to see if global local WER collection is enabled
    // See https://learn.microsoft.com/windows/win32/wer/collecting-user-mode-dumps
    // for more details
    internal static bool IsGlobalCollectionEnabled()
    {
        var key = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, false);

        return IsCollectionEnabledForKey(key);
    }

    // See if local WER collection is enabled for a specific app
    internal static bool IsCollectionEnabledForApp(string appName)
    {
        var key = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, false);

        // If the local dump key doesn't exist, then app collection is disabled
        if (key is null)
        {
            return false;
        }

        var appKey = key.OpenSubKey(appName, false);

        // If the app key doesn't exist, per-app collection isn't enabled. Check the global setting
        if (appKey is null)
        {
            return IsGlobalCollectionEnabled();
        }

        return IsCollectionEnabledForKey(appKey);
    }

    internal static bool IsCollectionEnabledForKey(RegistryKey? key)
    {
        // If the key doesn't exist, then collection is disabled
        if (key is null)
        {
            return false;
        }

        // If the key exists, but dumpcount is set to 0, it's also disabled
        if (key.GetValue("DumpCount") is int dumpCount && dumpCount == 0)
        {
            return false;
        }

        // Collection is enabled enabled, but if we're not getting full memory dumps, so cabs may not be
        // useful. In this case, report that collection is disabled.
        var dumpType = key.GetValue("DumpType") as int?;
        if (dumpType is null || dumpType != 2)
        {
            return false;
        }

        // Otherwise it's enabled
        return true;
    }

    // This changes the registry keys necessary to allow local WER collection for a specific app
    internal static void EnableCollectionForApp(string appname)
    {
        var globalKey = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, true);

        if (globalKey is null)
        {
            // Need to create the key, and set the global dump collection count to 0 to prevent all apps from generating local dumps
            globalKey = Registry.LocalMachine.CreateSubKey(LocalWERRegistryKey);
            globalKey.SetValue("DumpCount", 0);
        }

        Debug.Assert(globalKey is not null, "Global key is null");

        var appKey = globalKey.CreateSubKey(appname);
        Debug.Assert(appKey is not null, "App key is null");

        // If dumpcount doesn't exist or is set to 0, set the default value to get cabs
        if (appKey.GetValue("DumpCount") is not int dumpCount || dumpCount == 0)
        {
            appKey.SetValue("DumpCount", 10);
        }

        // Make sure the cabs being collected are useful. Go for the full dumps instead of the mini dumps
        appKey.SetValue("DumpType", 2);
        return;
    }

    // This changes the registry keys necessary to disable local WER collection for a specific app
    internal static void DisableCollectionForApp(string appname)
    {
        var globalKey = Registry.LocalMachine.OpenSubKey(LocalWERRegistryKey, true);

        if (globalKey is null)
        {
            // Local collection isn't enabled
            return;
        }

        var appKey = globalKey.CreateSubKey(appname);
        Debug.Assert(appKey is not null, "App key is null");

        // Set the DumpCount value to 0 to disable collection
        appKey.SetValue("DumpCount", 0);

        return;
    }
}
