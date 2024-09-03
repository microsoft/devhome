// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.RestartManager;

namespace DevHome.DevDiagnostics.Helpers;

internal static class RestartManagerHelper
{
    // Find out what process(es) have a lock on the specified file.
    internal static WIN32_ERROR GetLockingProcesses(string filePath, out List<Process> processes)
    {
        var key = Guid.NewGuid().ToString();
        processes = [];

        // Start a Restart Manager session.
        var result = WIN32_ERROR.ERROR_SUCCESS;
        uint handle;
        unsafe
        {
            fixed (char* p = key)
            {
                PInvoke.RmStartSession(out handle, p);
            }
        }

        if (result != 0)
        {
            return result;
        }

        try
        {
            uint pnProcInfo = 0;
            var lpdwRebootReasons = (uint)RM_REBOOT_REASON.RmRebootReasonNone;

            unsafe
            {
                fixed (char* p = filePath)
                {
                    var filePathStr = new PCWSTR(p);
                    var resources = new ReadOnlySpan<PCWSTR>(&filePathStr, 1);
                    var uniqueProcesses = default(Span<RM_UNIQUE_PROCESS>);
                    var serviceNames = default(ReadOnlySpan<PCWSTR>);

                    // Specify the given file as a resource to be managed by the Restart Manager.
                    result = PInvoke.RmRegisterResources(handle, resources, uniqueProcesses, serviceNames);
                    if (result != 0)
                    {
                        return result;
                    }
                }
            }

            // Note: there's a race here - the first call to RmGetList returns the count of processes,
            // but when we call RmGetList again to get them this number might have changed.
            unsafe
            {
                result = PInvoke.RmGetList(handle, out var pnProcInfoNeeded, ref pnProcInfo, null, out lpdwRebootReasons);
                if (result == WIN32_ERROR.ERROR_MORE_DATA)
                {
                    var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];

                    fixed (RM_PROCESS_INFO* processArrayPtr = processInfo)
                    {
                        pnProcInfo = pnProcInfoNeeded;

                        // Get the list of running processes that are using the given resource (file).
                        result = PInvoke.RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processArrayPtr, out lpdwRebootReasons);
                        if (result == 0)
                        {
                            // Enumerate all of the returned PIDS, get a Process for each one, and add it to the list.
                            processes = new List<Process>((int)pnProcInfo);
                            for (var i = 0; i < pnProcInfo; i++)
                            {
                                try
                                {
                                    processes.Add(Process.GetProcessById((int)processInfo[i].Process.dwProcessId));
                                }
                                catch (ArgumentException)
                                {
                                    // The process might have died before we got to look at it.
                                }
                            }
                        }
                        else
                        {
                            return result;
                        }
                    }
                }
                else if (result != 0)
                {
                    return result;
                }
            }
        }
        finally
        {
            _ = PInvoke.RmEndSession(handle);
        }

        return 0;
    }
}
