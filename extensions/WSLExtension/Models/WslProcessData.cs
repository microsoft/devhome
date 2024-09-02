// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Text;
using static WSLExtension.Constants;

namespace WSLExtension.Models;

/// <summary>
/// Represents metadata about a process that has exited.
/// </summary>
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

    public bool ExitedSuccessfully()
    {
        return ExitCode == WslExeExitSuccess;
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.AppendLine(CultureInfo.InvariantCulture, $"ExitCode: {ExitCode} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"StdOutput: {StdOutput} ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"StdError: {StdError} ");
        return builder.ToString();
    }
}
