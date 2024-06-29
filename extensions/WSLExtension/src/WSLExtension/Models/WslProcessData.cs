// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace WSLExtension.Models;

public class WslProcessData
{
    public int ExitCode { get; }

    public string StdOutput { get; } = string.Empty;

    public string StdError { get; } = string.Empty;

    public WslProcessData(int exitCode)
    {
        ExitCode = exitCode;
    }

    public WslProcessData(int exitCode, string stdOutput, string stdError)
    {
        ExitCode = exitCode;
        StdOutput = stdOutput;
        StdError = stdError;
    }
}
