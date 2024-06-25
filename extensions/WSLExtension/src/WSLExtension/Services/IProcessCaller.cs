// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;

namespace WSLExtension.Services;

public interface IProcessCaller
{
    string CallProcess(string command, string arguments, string? workingDirectory = null);

    string CallProcess(string command, string arguments, Encoding outputEncoding, string? workingDirectory = null);

    string CallProcess(string command, string arguments, Encoding outputEncoding, out int exitCode, string? workingDirectory = null);

    string CallProcess(string command, string arguments, out int exitCode, string? workingDirectory = null);

    void CallDetachedProcess(string command, string arguments, bool useShell = false);

    string RunCmdInDistro(string distroRegistration, string command, out int exitCode, bool root = false, string? stdIn = null);

    string RunCmdInDistro(string distroRegistration, string command, bool root = false);

    string CallElevatedProcess(string command, string arguments, Encoding outputEncoding, string? workingDirectory = null);

    string RunCmdInDistroDetached(string distroRegistration, string command, bool root = false, string? stdIn = null);

    Task<int> CallInteractiveProcess(string command, string arguments);
}
