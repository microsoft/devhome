// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WSLExtension.Models;

public class WslProcessData
{
    public int ExitCode { get; }

    public string StdOutput { get; }

    public string StdError { get; }

    public WslProcessData(int exitCode, string stdOutput, string stdError)
    {
        ExitCode = exitCode;
        StdOutput = stdOutput;
        StdError = stdError;
    }
}
