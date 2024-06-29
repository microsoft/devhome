// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Text;
using WSLExtension.Models;

namespace WSLExtension.Services;

public interface IProcessCaller
{
    public Process CreateProcessWithWindow(string fileName, string arguments);

    public WslProcessData CreateProcessWithoutWindow(string fileName, string arguments);

    public string CallProcess(string command, string arguments, out int exitCode, string? workingDirectory = null);
}
