// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Windows.Win32.Foundation;

namespace DevHome.PI.Helpers;

public class LoaderSnapAssistantTool
{
    private const string WindowsImageETWProvider = "2cb15d1d-5fc1-11d2-abe1-00a0c911f518"; /*EP_Microsoft-Windows-ImageLoad*/
    private const uint LoaderSnapsFlag = 0x80; /* ETW_UMGL_LDR_SNAPS_FLAG */

    public LoaderSnapAssistantTool()
    {
        Init();
    }

    private void Init()
    {
        var crashDumpAnalyzerThread = new Thread(() =>
        {
            MyETWListener();
        });
        crashDumpAnalyzerThread.Name = "LoaderSnapAssistantThread";
        crashDumpAnalyzerThread.Start();
    }

    private void MyETWListener()
    {
        TraceEventSession session = new TraceEventSession("LoaderSnapAssistantSession");

        // First enable the kernel provider to look for processes exiting with non-zero exit codes
        session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

        // Enablthe loader snaps provider
        session.EnableProvider(WindowsImageETWProvider, TraceEventLevel.Error, LoaderSnapsFlag);

        session.Source.Kernel.ProcessStop += data =>
        {
            if (data.ExitStatus == NTSTATUS.STATUS_DLL_NOT_FOUND || data.ExitStatus == (int)WIN32_ERROR.ERROR_MOD_NOT_FOUND)
            {
                Console.WriteLine("Process Ending {0,6} Process Name {1}, Exit Code {2}", data.ProcessID, data.ImageFileName, data.ExitStatus);

                // Need to write the following registry key to collect more information
                // Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\data.ImageFileName
                // TracingFlags = 0x4
            }
        };

        // We don't care about a lot of the ETW data that is coming in, so we just hook up the All event and ignore it
        session.Source.Dynamic.All += data => { };

        // None of the loadersnap events are handled by the TraceEventParser, so we need to handle them ourselves
        session.Source.UnhandledEvents += UnHandledEventsHandler;
        session.Source.Process();
    }

    private static void UnHandledEventsHandler(TraceEvent traceEvent)
    {
        if (traceEvent.EventName.Contains("Opcode(215)"))
        {
            byte[] loaderSnapData = traceEvent.EventData();
            string s = System.Text.Encoding.Unicode.GetString(loaderSnapData.Skip(10).ToArray());
            s = s.Replace("\n\0", string.Empty);
            s = s.Replace("\0", ": ");
            if (s.Contains("LdrpProcessWork - ERROR: Unable to load"))
            {
                Console.WriteLine(traceEvent.ProcessName);
                Console.WriteLine(s);
            }
        }
    }
}
