// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Windows.Win32.Foundation;

namespace DevHome.Service.Runtime;

[ComVisible(true)]
public class DevHomeService : IDevHomeService
{
    public event MissingFileProcessLaunchFailureHandler? MissingFileProcessLaunchFailure;

    public DevHomeService()
    {
        ComHelpers.VerifyCaller();

        // Track our caller process
        ServiceLifetimeController.RegisterProcess(ComHelpers.GetClientProcess());

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

        // Enable the kernel provider to look for processes exiting with non-zero exit codes
        session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

        session.Source.Kernel.ProcessStop += data =>
        {
            if (data.ExitStatus == NTSTATUS.STATUS_DLL_NOT_FOUND || data.ExitStatus == (int)WIN32_ERROR.ERROR_MOD_NOT_FOUND)
            {
                MissingFileProcessLaunchFailureInfo info = default(MissingFileProcessLaunchFailureInfo);
                info.processName = data.ImageFileName;
                info.pid = data.ProcessID;
                info.exitCode = data.ExitStatus;

                try
                {
                    MissingFileProcessLaunchFailure?.Invoke(info);
                }
                catch (Exception)
                {
                    // We don't want to crash the process if the event handler throws an exception
                }
            }
        };

        // None of the loadersnap events are handled by the TraceEventParser, so we need to handle them ourselves
        session.Source.Process();
    }
}
