// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Win32;

namespace EnableLoaderSnaps;

internal sealed class EnableLoaderSnaps
{
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

        int? tracingFlags = key.GetValue("TracingFlags") as int?;

        if (tracingFlags is null)
        {
            key.SetValue("TracingFlags", 0x4, RegistryValueKind.DWord);
        }
        else
        {
            key.SetValue("TracingFlags", tracingFlags | 0x4, RegistryValueKind.DWord);
        }

        return;
    }
}
