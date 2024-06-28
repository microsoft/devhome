// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;

namespace WSLExtension.Services;

public interface IProcessCaller
{
    public Process CallInteractiveProcess(string fileName, string arguments);

    public string CallProcess(string command, string arguments, out int exitCode, string? workingDirectory = null);

    public void CallDetachedProcess(string command, string arguments, bool useShell = false);
}
