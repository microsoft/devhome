// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json.Nodes;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Authentication.Identity;

namespace HyperVExtension.DevSetupAgent;

/// <summary>
/// Class used to handle request for service version (RequestType = GetVersion).
/// </summary>
internal sealed class IsUserLoggedInRequest : RequestBase
{
    public IsUserLoggedInRequest(IRequestContext requestContext)
        : base(requestContext)
    {
    }

    public override bool IsStatusRequest => true;

    public override IHostResponse Execute(ProgressHandler progressHandler, CancellationToken stoppingToken)
    {
        var loggedInUsers = EnumerateLogonSessions();
        return new IsUserLoggedInResponse(RequestMessage.RequestId!, loggedInUsers);
    }

    private static List<string> EnumerateLogonSessions()
    {
        // We'll take interactive sessions where explorer is running to filter out stale sessions
        var interactiveSessions = new List<string>();

        var explorers = Process.GetProcessesByName("explorer");
        if (explorers.Length == 0)
        {
            return interactiveSessions;
        }

        unsafe
        {
            var sessions = new List<(uint, string)>(); // (SessionId, UserName)
            LUID* luidPtr = default;
            SECURITY_LOGON_SESSION_DATA* sessionData = default;
            try
            {
                uint count;
                var status = PInvoke.LsaEnumerateLogonSessions(out count, out luidPtr);
                if (status != NTSTATUS.STATUS_SUCCESS)
                {
                    throw new NtStatusException("LsaEnumerateLogonSessions failed.", status);
                }

                Logging.Logger()?.ReportDebug($"Number of logon sessions: {count}");
                for (var i = 0; i < count; i++)
                {
                    var luid = luidPtr[i];
                    status = PInvoke.LsaGetLogonSessionData(luid, out sessionData);
                    if (status != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw new NtStatusException("LsaGetLogonSessionData failed.", status);
                    }

                    if (sessionData->Session > 0)
                    {
                        switch (sessionData->LogonType)
                        {
                            case (uint)SECURITY_LOGON_TYPE.Interactive:
                            case (uint)SECURITY_LOGON_TYPE.RemoteInteractive:
                            case (uint)SECURITY_LOGON_TYPE.CachedRemoteInteractive:
                                var sid = new SecurityIdentifier((IntPtr)sessionData->Sid.Value);
                                Logging.Logger()?.ReportDebug(
                                    $"Logged on user: {sessionData->UserName.Buffer}, " +
                                    $"Domain: {sessionData->LogonDomain.Buffer}, " +
                                    $"Session: {sessionData->Session}, " +
                                    $"Logon type: {sessionData->LogonType}, " +
                                    $"SID: {sid}, " +
                                    $"UserFlags: {sessionData->UserFlags}");

                                sessions.Add((sessionData->Session, new string(sessionData->UserName.Buffer)));
                                break;

                            default:
                                break;
                        }
                    }

                    PInvoke.LsaFreeReturnBuffer(sessionData);
                    sessionData = default;
                }

                // We'll take interactive sessions where explorer is running to filter out stale sessions
                var interactiveSessionsWithExplorer = sessions.Where(s => explorers.Any(e => (uint)e.SessionId == s.Item1));
                foreach (var session in interactiveSessionsWithExplorer)
                {
                    // TODO: We get OS user names like "DWM-2" or "UMFD-2" into this list. We need to filter them out.
                    Logging.Logger()?.ReportDebug($"Logged on user: {session.Item2}, Session: {session.Item1}");
                    interactiveSessions.Add(session.Item2);
                }
            }
            finally
            {
                if (sessionData != default)
                {
                    PInvoke.LsaFreeReturnBuffer(sessionData);
                }

                if (luidPtr != default)
                {
                    PInvoke.LsaFreeReturnBuffer(luidPtr);
                }
            }
        }

        return interactiveSessions;
    }
}
