// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Win32;

namespace EnableLoaderSnaps;

internal sealed class EnableLoaderSnaps
{
    [Flags]
    private enum TraceFlags
    {
        HeapTracing = 1,
        CritSecTracing = 2,
        LoaderSnaps = 4,
    }

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Debug.WriteLine("Unexpected command line");
            return;
        }

        EnableLoaderSnapLoggingForImage(args[0]);
    }

    private static void EnableLoaderSnapLoggingForImage(string imageFileName)
    {
        // Set the following flag in the registry key
        // Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\imageFileName
        // TracingFlags = 0x4

        // Create this as volatile so that it doesn't persist after a reboot
        RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\" + imageFileName, true, RegistryOptions.Volatile);

        TraceFlags? tracingFlags = key.GetValue("TracingFlags") as TraceFlags?;

        if (tracingFlags is null)
        {
            key.SetValue("TracingFlags", TraceFlags.LoaderSnaps, RegistryValueKind.DWord);
        }
        else
        {
            key.SetValue("TracingFlags", tracingFlags | TraceFlags.LoaderSnaps, RegistryValueKind.DWord);
        }

        return;
    }
}
