// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Windows.Win32.Foundation;

namespace DevHome.Service.Runtime;

[ComVisible(true)]
public class DevHomeService : IDevHomeService, IDisposable
{
    private readonly Process _owner;

    public event MissingFileProcessLaunchFailureHandler? MissingFileProcessLaunchFailure;

    private TraceEventSession? _etwSession;

    public DevHomeService()
    {
        Process myCaller = ComHelpers.GetClientProcess();
        ComHelpers.VerifyCaller(myCaller);

        _owner = myCaller;
        _owner.EnableRaisingEvents = true;

        // Track our caller process
        ServiceLifetimeController.ServiceStop += ServiceLifetimeController_ServiceStop;
        ServiceLifetimeController.RegisterProcess(_owner);

        var crashDumpAnalyzerThread = new Thread(() =>
        {
            KernelEventETWListener();
        });
        crashDumpAnalyzerThread.Name = "KernelEventETWListenerThread";
        crashDumpAnalyzerThread.Start();

        _owner.Exited += Owner_Exited;
    }

    private void ServiceLifetimeController_ServiceStop()
    {
        // Be sure to stop our ETW session when we exit. It's possible this gets called multiple times
        // if we have multiple instances of our object... that's ok.
        _etwSession?.Stop();
    }

    private void Owner_Exited(object? sender, EventArgs e)
    {
        _etwSession?.Source.StopProcessing();
    }

    private void KernelEventETWListener()
    {
        _etwSession = new TraceEventSession("DevHome.Service.KernelEventETWListenerSession");

        // Enable the kernel provider to look for processes exiting with non-zero exit codes
        _etwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

        _etwSession.Source.Kernel.ProcessStop += data =>
        {
            // Only return data for processes in session 0 or the caller's session (don't let one session spy on another session)
            if (data.SessionID == 0 || data.SessionID == _owner.SessionId)
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
            }
        };

        _etwSession.Source.Process();
    }

    public void Dispose()
    {
        _etwSession?.Source.StopProcessing();
        _etwSession?.Dispose();
        GC.SuppressFinalize(this);
    }
}
